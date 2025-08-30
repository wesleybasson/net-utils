namespace ApiX.Signals.Core.Buffers;

/// <summary>
/// 
/// </summary>
/// <typeparam name="T"></typeparam>
public sealed class RingBuffer<T>
{
    private readonly T[] _buf;
    private int _head; // next write
    private int _count;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="capacity"></param>
    public RingBuffer(int capacity)
    {
        _buf = new T[capacity];
    }

    /// <summary>
    /// 
    /// </summary>
    public int Count => _count;

    /// <summary>
    /// 
    /// </summary>
    public int Capacity => _buf.Length;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="value"></param>
    public void Push(T value)
    {
        _buf[_head] = value;
        _head = (_head + 1) % _buf.Length;
        if (_count < _buf.Length) _count++;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="dst"></param>
    /// <exception cref="ArgumentException"></exception>
    public void CopyTo(Span<T> dst)
    {
        if (dst.Length < _count) throw new ArgumentException("dst too small");
        int cap = _buf.Length;
        int start = (_head - _count + cap) % cap;
        if (start + _count <= cap)
        {
            _buf.AsSpan(start, _count).CopyTo(dst);
        }
        else
        {
            int first = cap - start;
            _buf.AsSpan(start, first).CopyTo(dst[..first]);
            _buf.AsSpan(0, _count - first).CopyTo(dst[first.._count]);
        }
    }
}
