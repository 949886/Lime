using System;
using System.Threading.Tasks;
using Godot;
using Lime.Game.Actors.Player;
using Lime.Game.Camera;
using Lime.Game.World.Levels.Reference;

namespace Lime.Diagnostics.Reference;

public partial class ReferenceComparator : Control
{
    private const float OverlayAlpha = 0.5f;

    [Export] public NodePath PlayerPath { get; set; } = new();
    [Export] public NodePath ReferenceLevelPath { get; set; } = new();
    [Export] public NodePath CameraDirectorPath { get; set; } = new();

    [Export] public ReferenceCheckpoint StartCheckpoint { get; set; } = null!;
    [Export] public ReferenceCheckpoint Check01Checkpoint { get; set; } = null!;
    [Export] public ReferenceCheckpoint Check02Checkpoint { get; set; } = null!;
    [Export] public ReferenceCheckpoint Check03Checkpoint { get; set; } = null!;
    [Export] public ReferenceCheckpoint RouteEndCheckpoint { get; set; } = null!;

    private PlayerController _player = null!;
    private ReferenceLevel _referenceLevel = null!;
    private CameraDirector _cameraDirector = null!;
    private TextureRect _referenceLayer = null!;
    private ColorRect _differenceLayer = null!;
    private ShaderMaterial _differenceMaterial = null!;
    private Label _checkpointLabel = null!;
    private Label _statusLabel = null!;
    private Button _previousButton = null!;
    private Button _nextButton = null!;
    private Button _teleportButton = null!;
    private Button _perspectiveButton = null!;
    private Button _orthographicButton = null!;
    private Button _liveButton = null!;
    private Button _referenceButton = null!;
    private Button _overlayButton = null!;
    private Button _differenceButton = null!;
    private Button _captureButton = null!;

    private ReferenceCheckpoint[] _checkpoints = Array.Empty<ReferenceCheckpoint>();

    public int CurrentCheckpointIndex { get; private set; }
    public ReferenceCheckpoint CurrentCheckpoint => _checkpoints[CurrentCheckpointIndex];
    public int CheckpointCount => _checkpoints.Length;
    public ReferenceComparisonMode ComparisonMode { get; private set; } = ReferenceComparisonMode.Overlay;
    public ImageTexture? CurrentCapture { get; private set; }

    public override void _Ready()
    {
        ResolveDependencies();
        ResolveUi();
        BuildCheckpointCatalog();
        WireUi();
        SelectCheckpoint(0);
        ApplyComparisonMode();
    }

    public override void _ExitTree()
    {
        if (_previousButton is null)
        {
            return;
        }

        _previousButton.Pressed -= SelectPreviousCheckpoint;
        _nextButton.Pressed -= SelectNextCheckpoint;
        _teleportButton.Pressed -= TeleportToCurrentCheckpoint;
        _perspectiveButton.Pressed -= ActivatePerspective;
        _orthographicButton.Pressed -= ActivateOrthographic;
        _liveButton.Pressed -= ActivateLiveMode;
        _referenceButton.Pressed -= ActivateReferenceMode;
        _overlayButton.Pressed -= ActivateOverlayMode;
        _differenceButton.Pressed -= ActivateDifferenceMode;
        _captureButton.Pressed -= CaptureCurrent;
    }

    public override void _UnhandledKeyInput(InputEvent @event)
    {
        if (@event is InputEventKey keyEvent &&
            keyEvent.Pressed &&
            !keyEvent.Echo &&
            keyEvent.Keycode == Key.F9 &&
            keyEvent.CtrlPressed &&
            keyEvent.ShiftPressed)
        {
            Visible = !Visible;
            GetViewport().SetInputAsHandled();
        }
    }

    public ReferenceCheckpoint GetCheckpoint(int index)
    {
        if ((uint)index >= (uint)_checkpoints.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(index), index, "Reference checkpoint index is out of range.");
        }

        return _checkpoints[index];
    }

    public void SelectCheckpoint(int index)
    {
        if ((uint)index >= (uint)_checkpoints.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(index), index, "Reference checkpoint index is out of range.");
        }

        CurrentCheckpointIndex = index;
        _checkpointLabel.Text = BuildCheckpointLabel(CurrentCheckpoint);
        RefreshReferenceTexture();
        _statusLabel.Text = "Checkpoint selected.";
    }

    public void SelectPreviousCheckpoint()
    {
        var index = (CurrentCheckpointIndex - 1 + _checkpoints.Length) % _checkpoints.Length;
        SelectCheckpoint(index);
        TeleportToCurrentCheckpoint();
    }

    public void SelectNextCheckpoint()
    {
        var index = (CurrentCheckpointIndex + 1) % _checkpoints.Length;
        SelectCheckpoint(index);
        TeleportToCurrentCheckpoint();
    }

    public void TeleportToCurrentCheckpoint()
    {
        var marker = ResolveCurrentMarker();
        _player.GlobalPosition = marker.GlobalPosition;
        _player.Velocity = Vector3.Zero;

        var cameraId = CurrentCheckpoint.Id == ReferenceCheckpointId.Start
            ? CameraId.StartPerspective
            : CameraId.ExplorePerspective;
        _cameraDirector.ActivateInstant(cameraId);

        _statusLabel.Text = $"Teleported to {CurrentCheckpoint.Id}.";
    }

    public void ActivateCamera(CameraId id)
    {
        _cameraDirector.ActivateInstant(id);
        _statusLabel.Text = $"Camera: {id}.";
    }

    public void SetComparisonMode(ReferenceComparisonMode mode)
    {
        ComparisonMode = mode;
        ApplyComparisonMode();
    }

    public async Task<ImageTexture> CaptureCurrentAsync()
    {
        if (string.Equals(DisplayServer.GetName(), "headless", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Viewport capture is unavailable while Godot uses the headless display server.");
        }

        var wasVisible = Visible;
        Visible = false;

        try
        {
            await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);

            var image = GetViewport().GetTexture().GetImage()
                ?? throw new InvalidOperationException("Viewport capture returned no image.");
            var capture = ImageTexture.CreateFromImage(image);

            CurrentCapture = capture;
            if (_statusLabel is not null)
            {
                _statusLabel.Text = $"Captured {image.GetWidth()} × {image.GetHeight()}.";
            }

            return capture;
        }
        finally
        {
            Visible = wasVisible;
        }
    }

    private void ResolveDependencies()
    {
        _player = GetNode<PlayerController>(PlayerPath);
        _referenceLevel = GetNode<ReferenceLevel>(ReferenceLevelPath);
        _cameraDirector = GetNode<CameraDirector>(CameraDirectorPath);
    }

    private void ResolveUi()
    {
        _referenceLayer = GetNode<TextureRect>("%ReferenceLayer");
        _differenceLayer = GetNode<ColorRect>("%DifferenceLayer");
        _differenceMaterial = _differenceLayer.Material as ShaderMaterial
            ?? throw new InvalidOperationException("DifferenceLayer must use a ShaderMaterial.");

        _checkpointLabel = GetNode<Label>("%CheckpointLabel");
        _statusLabel = GetNode<Label>("%StatusLabel");
        _previousButton = GetNode<Button>("%PreviousButton");
        _nextButton = GetNode<Button>("%NextButton");
        _teleportButton = GetNode<Button>("%TeleportButton");
        _perspectiveButton = GetNode<Button>("%PerspectiveButton");
        _orthographicButton = GetNode<Button>("%OrthographicButton");
        _liveButton = GetNode<Button>("%LiveButton");
        _referenceButton = GetNode<Button>("%ReferenceButton");
        _overlayButton = GetNode<Button>("%OverlayButton");
        _differenceButton = GetNode<Button>("%DifferenceButton");
        _captureButton = GetNode<Button>("%CaptureButton");
    }

    private void BuildCheckpointCatalog()
    {
        _checkpoints =
        [
            StartCheckpoint,
            Check01Checkpoint,
            Check02Checkpoint,
            Check03Checkpoint,
            RouteEndCheckpoint,
        ];

        for (var index = 0; index < _checkpoints.Length; index++)
        {
            var checkpoint = _checkpoints[index]
                ?? throw new InvalidOperationException($"Reference checkpoint slot {index} is not assigned.");

            if (checkpoint.Frame is null)
            {
                throw new InvalidOperationException($"Reference checkpoint {checkpoint.Id} has no ReferenceFrame.");
            }

            if (checkpoint.Frame.Image is null)
            {
                throw new InvalidOperationException($"Reference checkpoint {checkpoint.Id} has no reference image.");
            }

            if (checkpoint.MarkerPath.ToString().Length == 0)
            {
                throw new InvalidOperationException($"Reference checkpoint {checkpoint.Id} has no MarkerPath.");
            }
        }
    }

    private Marker3D ResolveCurrentMarker()
    {
        return _referenceLevel.GetNodeOrNull<Marker3D>(CurrentCheckpoint.MarkerPath)
            ?? throw new InvalidOperationException(
                $"Reference marker '{CurrentCheckpoint.MarkerPath}' for {CurrentCheckpoint.Id} could not be resolved.");
    }

    private void WireUi()
    {
        _previousButton.Pressed += SelectPreviousCheckpoint;
        _nextButton.Pressed += SelectNextCheckpoint;
        _teleportButton.Pressed += TeleportToCurrentCheckpoint;
        _perspectiveButton.Pressed += ActivatePerspective;
        _orthographicButton.Pressed += ActivateOrthographic;
        _liveButton.Pressed += ActivateLiveMode;
        _referenceButton.Pressed += ActivateReferenceMode;
        _overlayButton.Pressed += ActivateOverlayMode;
        _differenceButton.Pressed += ActivateDifferenceMode;
        _captureButton.Pressed += CaptureCurrent;
    }

    private void RefreshReferenceTexture()
    {
        var texture = CurrentCheckpoint.Frame.Image;
        _referenceLayer.Texture = texture;
        _differenceMaterial.SetShaderParameter("reference_texture", texture);
    }

    private void ApplyComparisonMode()
    {
        _referenceLayer.Visible =
            ComparisonMode is ReferenceComparisonMode.Reference or ReferenceComparisonMode.Overlay;
        _referenceLayer.Modulate = new Color(1f, 1f, 1f,
            ComparisonMode == ReferenceComparisonMode.Overlay ? OverlayAlpha : 1f);
        _differenceLayer.Visible = ComparisonMode == ReferenceComparisonMode.Difference;
    }

    private static string BuildCheckpointLabel(ReferenceCheckpoint checkpoint)
    {
        var frame = checkpoint.Frame;
        return $"{checkpoint.Id}  |  {frame.TimestampSeconds:0.00}s  |  frame {frame.SourceFrame}";
    }

    private void ActivatePerspective() => ActivateCamera(
        CurrentCheckpoint.Id == ReferenceCheckpointId.Start
            ? CameraId.StartPerspective
            : CameraId.ExplorePerspective);
    private void ActivateOrthographic() => ActivateCamera(CameraId.ExploreOrthographic);
    private void ActivateLiveMode() => SetComparisonMode(ReferenceComparisonMode.Live);
    private void ActivateReferenceMode() => SetComparisonMode(ReferenceComparisonMode.Reference);
    private void ActivateOverlayMode() => SetComparisonMode(ReferenceComparisonMode.Overlay);
    private void ActivateDifferenceMode() => SetComparisonMode(ReferenceComparisonMode.Difference);

    private async void CaptureCurrent()
    {
        try
        {
            await CaptureCurrentAsync();
        }
        catch (Exception exception)
        {
            GD.PushError($"Reference capture failed: {exception}");
            _statusLabel.Text = "Capture failed. See debugger output.";
        }
    }
}
