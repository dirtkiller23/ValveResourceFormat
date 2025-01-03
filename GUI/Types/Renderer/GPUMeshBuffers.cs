
using System.Buffers;
using System.IO;
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
            GL.CreateBuffers(vbib.VertexBuffers.Count, VertexBuffers);

            for (var i = 0; i < vbib.VertexBuffers.Count; i++)
            {
                LoadGPUBuffer(VertexBuffers[i], vbib.VertexBuffers[i], vbib.Reader);
            }

            IndexBuffers = new int[vbib.IndexBuffers.Count];
            GL.CreateBuffers(vbib.IndexBuffers.Count, IndexBuffers);

            for (var i = 0; i < vbib.IndexBuffers.Count; i++)
            {
                LoadGPUBuffer(IndexBuffers[i], vbib.IndexBuffers[i], vbib.Reader);
            }
        }

        private static void LoadGPUBuffer(int gpuBufferHandle, VBIB.OnDiskBufferData diskBuffer, BinaryReader reader)
        {
            // Data already exists in CPU, as a byte array.
            // This is common on older models that have keyvalues based VBIB.
            if (diskBuffer.Data != null)
            {
                GL.NamedBufferData(gpuBufferHandle, (IntPtr)diskBuffer.TotalSize, diskBuffer.Data, BufferUsageHint.StaticDraw);
                return;
            }

            // Data needs to be read from the resource stream.
            var data = ArrayPool<byte>.Shared.Rent((int)diskBuffer.TotalSize);
            diskBuffer.ReadFromResourceStream(reader, data.AsSpan());

            try
            {
                GL.NamedBufferData(gpuBufferHandle, (IntPtr)diskBuffer.TotalSize, data, BufferUsageHint.StaticDraw);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(data);
            }

        }
    }
}
