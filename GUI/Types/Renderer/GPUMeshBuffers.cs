
using System.Buffers;
using OpenTK.Graphics.OpenGL;
using ValveResourceFormat.Blocks;
using ValveResourceFormat.Compression;

namespace GUI.Types.Renderer
{
    class GPUMeshBuffers
    {
        public int[] VertexBuffers { get; private set; }
        public int[] IndexBuffers { get; private set; }

        public GPUMeshBuffers(VBIB vbib)
        {
            VertexBuffers = new int[vbib.VertexBuffers.Count];
            GL.CreateBuffers(vbib.VertexBuffers.Count, VertexBuffers);

            for (var i = 0; i < vbib.VertexBuffers.Count; i++)
            {
                LoadGPUBuffer(VertexBuffers[i], vbib.VertexBuffers[i]);
            }

            IndexBuffers = new int[vbib.IndexBuffers.Count];
            GL.CreateBuffers(vbib.IndexBuffers.Count, IndexBuffers);

            for (var i = 0; i < vbib.IndexBuffers.Count; i++)
            {
                LoadGPUBuffer(IndexBuffers[i], vbib.IndexBuffers[i]);
            }
        }

        private static void LoadGPUBuffer(int gpuBufferHandle, VBIB.OnDiskBufferData diskBuffer)
        {
            if (!diskBuffer.IsCompressed)
            {
                GL.NamedBufferData(gpuBufferHandle, (IntPtr)diskBuffer.TotalSizeInBytes, diskBuffer.RawData, BufferUsageHint.StaticDraw);
                return;
            }

            var uncompressed = ArrayPool<byte>.Shared.Rent((int)diskBuffer.TotalSizeInBytes);

            try
            {
                if (diskBuffer.IsVertex)
                {
                    MeshOptimizerVertexDecoder.DecodeVertexBuffer((int)diskBuffer.ElementCount, (int)diskBuffer.ElementSizeInBytes, diskBuffer.RawData, uncompressed.AsSpan(), useSimd: true);
                }
                else
                {
                    MeshOptimizerIndexDecoder.DecodeIndexBuffer((int)diskBuffer.ElementCount, (int)diskBuffer.ElementSizeInBytes, diskBuffer.RawData, uncompressed.AsSpan());
                }

                GL.NamedBufferData(gpuBufferHandle, (IntPtr)diskBuffer.TotalSizeInBytes, uncompressed, BufferUsageHint.StaticDraw);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(uncompressed);
            }
        }
    }
}
