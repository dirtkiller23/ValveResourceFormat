
using System.Buffers;
using System.Runtime.CompilerServices;
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

            var totalCount = vbib.VertexBuffers.Count + vbib.IndexBuffers.Count;
            var rentedArrays = new byte[totalCount][];

            try
            {
                // Read buffers from file if necessary
                for (var i = 0; i < totalCount; i++)
                {
                    var buffer = i < vbib.VertexBuffers.Count ? vbib.VertexBuffers[i] : vbib.IndexBuffers[i - vbib.VertexBuffers.Count];
                    if (buffer.Data == null)
                    {
                        rentedArrays[i] = ArrayPool<byte>.Shared.Rent((int)buffer.TotalSize);
                        buffer.ReadFromResourceStream(vbib.Reader, rentedArrays[i].AsSpan());
                    }
                }

                // Load buffers into GPU
                for (var i = 0; i < vbib.VertexBuffers.Count; i++)
                {
                    LoadGPUBuffer(VertexBuffers[i], vbib.VertexBuffers[i]);
                }

                for (var i = 0; i < vbib.IndexBuffers.Count; i++)
                {
                    LoadGPUBuffer(IndexBuffers[i], vbib.IndexBuffers[i]);
                }
            }
            finally
            {
                // Return rented CPU buffers
                for (var i = 0; i < totalCount; i++)
                {
                    if (rentedArrays[i] != null)
                    {
                        ArrayPool<byte>.Shared.Return(rentedArrays[i]);
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void LoadGPUBuffer(int gpuBufferHandle, VBIB.OnDiskBufferData buffer)
        {
            GL.NamedBufferData(gpuBufferHandle, (IntPtr)buffer.TotalSize, buffer.Data, BufferUsageHint.StaticDraw);
        }
    }
}
