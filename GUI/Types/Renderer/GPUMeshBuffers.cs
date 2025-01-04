
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL;
using ValveResourceFormat.Blocks;

namespace GUI.Types.Renderer
{
    class GPUMeshBuffers
    {
        public int[] VertexBuffers { get; private set; }
        public int[] IndexBuffers { get; private set; }

        public GPUMeshBuffers(VBIB vbib)
        {
            VertexBuffers = new int[vbib.VertexBuffers.Count];
            IndexBuffers = new int[vbib.IndexBuffers.Count];

            GL.CreateBuffers(vbib.VertexBuffers.Count, VertexBuffers);
            GL.CreateBuffers(vbib.IndexBuffers.Count, IndexBuffers);

            var indexBufferStart = vbib.VertexBuffers.Count;
            var bufferCount = vbib.VertexBuffers.Count + vbib.IndexBuffers.Count;

            var rentedArrays = new byte[bufferCount][];
            var handles = new int[bufferCount];
            var buffers = new byte[bufferCount][];
            var sizes = new uint[bufferCount];

            var decodeTasks = new List<Task>(bufferCount);

            try
            {
                // Read buffers from file if necessary
                for (var i = 0; i < bufferCount; i++)
                {
                    var isVertexBuffer = i < indexBufferStart;
                    var indexBufferIndex = i - indexBufferStart;

                    handles[i] = isVertexBuffer ? VertexBuffers[i] : IndexBuffers[indexBufferIndex];

                    var diskBuffer = isVertexBuffer ? vbib.VertexBuffers[i] : vbib.IndexBuffers[indexBufferIndex];
                    sizes[i] = diskBuffer.TotalSize;
                    buffers[i] = diskBuffer.Data;

                    if (diskBuffer.Data == null)
                    {
                        buffers[i] = rentedArrays[i] = ArrayPool<byte>.Shared.Rent((int)diskBuffer.TotalSize);

                        var arrayCapture = rentedArrays[i];
                        var decodeTask = Task.Run(() => diskBuffer.ReadFromResourceStream(vbib.Reader, vbib.ReaderLock, arrayCapture.AsSpan()));
                        decodeTasks.Add(decodeTask);
                    }
                }

                // Wait for all meshoptimizer decode tasks to finish
                Task.WaitAll(decodeTasks.ToArray());

                // Load buffers into GPU
                for (var i = 0; i < bufferCount; i++)
                {
                    LoadGPUBuffer(handles[i], buffers[i], sizes[i]);
                }
            }
            finally
            {
                // Return rented CPU buffers
                for (var i = 0; i < bufferCount; i++)
                {
                    if (rentedArrays[i] != null)
                    {
                        ArrayPool<byte>.Shared.Return(rentedArrays[i]);
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void LoadGPUBuffer(int gpuBufferHandle, byte[] buffer, uint size)
        {
            GL.NamedBufferData(gpuBufferHandle, (IntPtr)size, buffer, BufferUsageHint.StaticDraw);
        }
    }
}
