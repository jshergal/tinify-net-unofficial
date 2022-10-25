using System;
using System.IO;
using System.Threading.Tasks;

namespace Tinify.Unofficial.Tests
{
    public sealed class TempFile : IDisposable, IAsyncDisposable
    {
        public string Path { get; private set; } = System.IO.Path.GetTempFileName();

        ~TempFile() => Dispose(false);

        public async ValueTask DisposeAsync()
        {
            await Task.Run(() => Dispose(false));
            GC.SuppressFinalize(this);
        }

        public void Dispose() => Dispose(true);

        private void Dispose(bool disposing)
        {
            if (disposing) GC.SuppressFinalize(this);
            try
            {
                if (!string.IsNullOrEmpty(Path)) File.Delete(Path);
            }
            catch
            {
                // ignored
            }

            Path = null;
        }
    }
}
