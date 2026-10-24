using System.IO.MemoryMappedFiles;
using QpEnhance.Core;

namespace QpEnhance.Module;

/// <summary>One decoded frame from Virtual Desktop's shared-memory face state.</summary>
public sealed class VdFrame
{
    public readonly float[] Weights = new float[Fb2Constants.Count];
    public bool LowerFaceValid;
    public bool EyeExpressionValid;
    public bool LeftEyeValid;
    public bool RightEyeValid;
    /// <summary>Raw per-eye gaze derived from the eye-orientation quaternions (radians).</summary>
    public GazeSample Gaze;

    public bool AnyEyeValid => LeftEyeValid || RightEyeValid;
}

/// <summary>
/// Reads the stock, un-rooted Quest Pro face/eye signal that Virtual Desktop publishes
/// in its <c>VirtualDesktop.BodyState</c> shared memory (360 bytes). This is read-only
/// and touches nothing but that mapping; it does not root the headset or access cameras.
///
/// Layout (little-endian), confirmed against Virtual Desktop's own consumers:
///   byte 0        face flags (bit 0 = lower-face valid)
///   byte 1        eye-expression valid (squint / wide)
///   bytes 4..283  70 × float  FB2 expression weights
///   byte 292      left-eye pose valid
///   byte 293      right-eye pose valid
///   bytes 296..311  left-eye orientation quaternion  (x, y, z, w)
///   bytes 324..339  right-eye orientation quaternion (x, y, z, w)
/// </summary>
public sealed class VirtualDesktopFaceSource : IDisposable
{
    private const string MapName = "VirtualDesktop.BodyState";
    private const int StateBytes = 360;
    private const int ExpressionOffset = 4;
    private const int LeftEyeValidByte = 292;
    private const int RightEyeValidByte = 293;
    private const int LeftQuatOffset = 296;
    private const int RightQuatOffset = 324;

    private MemoryMappedFile? _map;
    private MemoryMappedViewAccessor? _view;
    private readonly byte[] _scratch = new byte[StateBytes];
    private readonly byte[] _buffer = new byte[StateBytes];
    private readonly VdFrame _frame = new();

    public bool Connected => _view is not null;

    /// <summary>Attempt to attach to the shared memory. Returns false if VD is not streaming yet.</summary>
    public bool TryConnect()
    {
        if (_view is not null)
            return true;
        try
        {
            _map = MemoryMappedFile.OpenExisting(MapName, MemoryMappedFileRights.Read);
            _view = _map.CreateViewAccessor(0, StateBytes, MemoryMappedFileAccess.Read);
            return true;
        }
        catch (FileNotFoundException)
        {
            Dispose();
            return false;
        }
        catch (Exception)
        {
            Dispose();
            return false;
        }
    }

    /// <summary>Read and decode the current frame, or null if not connected / read failed.</summary>
    public VdFrame? Read()
    {
        if (!TryConnect())
            return null;

        try
        {
            // Read twice and keep the second copy; it narrows the window for a torn
            // frame while the writer is mid-update (same approach VD's consumers use).
            _view!.ReadArray(0, _scratch, 0, StateBytes);
            _view!.ReadArray(0, _buffer, 0, StateBytes);
        }
        catch (Exception)
        {
            Dispose();
            return null;
        }

        for (int i = 0; i < Fb2Constants.Count; i++)
            _frame.Weights[i] = BitConverter.ToSingle(_buffer, ExpressionOffset + i * 4);

        byte faceFlags = _buffer[0];
        _frame.LowerFaceValid = (faceFlags & 1) != 0;
        _frame.EyeExpressionValid = _buffer[1] != 0;
        _frame.LeftEyeValid = _buffer[LeftEyeValidByte] != 0;
        _frame.RightEyeValid = _buffer[RightEyeValidByte] != 0;

        var (lx, ly) = _frame.LeftEyeValid ? Quat(LeftQuatOffset) : (0f, 0f);
        var (rx, ry) = _frame.RightEyeValid ? Quat(RightQuatOffset) : (0f, 0f);
        _frame.Gaze = new GazeSample(lx, ly, rx, ry);

        return _frame;
    }

    private (float X, float Y) Quat(int offset) => GazeMath.FromEyeQuaternion(
        BitConverter.ToSingle(_buffer, offset),
        BitConverter.ToSingle(_buffer, offset + 4),
        BitConverter.ToSingle(_buffer, offset + 8),
        BitConverter.ToSingle(_buffer, offset + 12));

    public void Dispose()
    {
        _view?.Dispose();
        _map?.Dispose();
        _view = null;
        _map = null;
    }
}
