namespace DiceWarrior.MockServer;

// Entity bytes successfully read/written, excluding headers, compression and transport delivery.
public sealed class MeasuredHttpStream(Stream inner) : Stream
{
    public long ReadBytes {get;private set;}
    public long WrittenBytes {get;private set;}
    public override bool CanRead=>inner.CanRead;
    public override bool CanSeek=>inner.CanSeek;
    public override bool CanWrite=>inner.CanWrite;
    public override long Length=>inner.Length;
    public override long Position {get=>inner.Position;set=>inner.Position=value;}
    public override void Flush()=>inner.Flush();
    public override Task FlushAsync(CancellationToken ct)=>inner.FlushAsync(ct);
    public override long Seek(long offset,SeekOrigin origin)=>inner.Seek(offset,origin);
    public override void SetLength(long value)=>inner.SetLength(value);
    public override int Read(byte[] buffer,int offset,int count) {int read=inner.Read(buffer,offset,count);ReadBytes+=read;return read;}
    public override async ValueTask<int> ReadAsync(Memory<byte> buffer,CancellationToken ct=default)
    {int read=await inner.ReadAsync(buffer,ct);ReadBytes+=read;return read;}
    public override Task<int> ReadAsync(byte[] buffer,int offset,int count,CancellationToken ct)=>ReadAsync(buffer.AsMemory(offset,count),ct).AsTask();
    public override void Write(byte[] buffer,int offset,int count) {inner.Write(buffer,offset,count);WrittenBytes+=count;}
    public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer,CancellationToken ct=default)
    {await inner.WriteAsync(buffer,ct);WrittenBytes+=buffer.Length;}
    public override Task WriteAsync(byte[] buffer,int offset,int count,CancellationToken ct)=>WriteAsync(buffer.AsMemory(offset,count),ct).AsTask();
    // Ownership remains with ASP.NET; disposing this wrapper never closes either body.
}
