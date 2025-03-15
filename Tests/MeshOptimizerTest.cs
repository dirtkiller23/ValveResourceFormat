using System.Runtime.InteropServices;
using NUnit.Framework;
using ValveResourceFormat.Compression;

// Copied from https://github.com/zeux/meshoptimizer/blob/master/demo/tests.cpp

namespace Tests
{
    public class MeshOptimizerTest
    {
        // note: 4 6 5 triangle here is a combo-breaker:
        // we encode it without rotating, a=next, c=next - this means we do *not* bump next to 6
        // which means that the next triangle can't be encoded via next sequencing!
        private static readonly int[] kIndexBuffer = [0, 1, 2, 2, 1, 3, 4, 6, 5, 7, 8, 9];

        private static readonly byte[] kIndexDataV0 = [
            0xe0, 0xf0, 0x10, 0xfe, 0xff, 0xf0, 0x0c, 0xff, 0x02, 0x02, 0x02, 0x00, 0x76, 0x87, 0x56, 0x67,
            0x78, 0xa9, 0x86, 0x65, 0x89, 0x68, 0x98, 0x01, 0x69, 0x00, 0x00,
        ];

        // note: this exercises two features of v1 format, restarts (0 1 2) and last
        private static readonly int[] kIndexBufferTricky = [0, 1, 2, 2, 1, 3, 0, 1, 2, 2, 1, 5, 2, 1, 4];

        private static readonly byte[] kIndexDataV1 = [
            0xe1, 0xf0, 0x10, 0xfe, 0x1f, 0x3d, 0x00, 0x0a, 0x00, 0x76, 0x87, 0x56, 0x67, 0x78, 0xa9, 0x86,
            0x65, 0x89, 0x68, 0x98, 0x01, 0x69, 0x00, 0x00,
        ];

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct PV
        {
            public ushort px, py, pz;
            public byte nu, nv;
            public ushort tx, ty;
        }

        private static readonly PV[] kVertexBuffer = [
            new() { px = 0, py = 0, pz = 0, nu = 0, nv = 0, tx = 0, ty = 0 },
            new() { px = 300, py = 0, pz = 0, nu = 0, nv = 0, tx = 500, ty = 0 },
            new() { px = 0, py = 300, pz = 0, nu = 0, nv = 0, tx = 0, ty = 500 },
            new() { px = 300, py = 300, pz = 0, nu = 0, nv = 0, tx = 500, ty = 500 },
        ];

        private static readonly byte[] kVertexDataV0 = [
            0xa0, 0x01, 0x3f, 0x00, 0x00, 0x00, 0x58, 0x57, 0x58, 0x01, 0x26, 0x00, 0x00, 0x00, 0x01,
            0x0c, 0x00, 0x00, 0x00, 0x58, 0x01, 0x08, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01,
            0x3f, 0x00, 0x00, 0x00, 0x17, 0x18, 0x17, 0x01, 0x26, 0x00, 0x00, 0x00, 0x01, 0x0c, 0x00,
            0x00, 0x00, 0x17, 0x01, 0x08, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        ];

        private static readonly byte[] kVertexDataV1 = [
            0xa1, 0xee, 0xaa, 0xee, 0x00, 0x4b, 0x4b, 0x4b, 0x00, 0x00, 0x4b, 0x00, 0x00, 0x7d, 0x7d, 0x7d,
            0x00, 0x00, 0x7d, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x62, 0x00, 0x62,
        ];

        // This binary blob is a valid v1 encoding of vertex buffer but it used a custom version of
        // the encoder that exercised all features of the format; because of this it is much larger
        // and will never be produced by the encoder itself.
        private static readonly byte[] kVertexDataV1Custom = [
            0xa1, 0xd4, 0x94, 0xd4, 0x01, 0x0e, 0x00, 0x58, 0x57, 0x58, 0x02, 0x02, 0x12, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x03, 0x00, 0x00, 0x58, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x02, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x02, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x03, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x0e, 0x00, 0x7d, 0x7d, 0x7d, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x7d, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x62,
        ];

        [Test]
        public void DecodeIndexV0()
        {
            var decoded = MeshOptimizerIndexDecoder.DecodeIndexBuffer(kIndexBuffer.Length, sizeof(int), kIndexDataV0);
            Assert.That(MemoryMarshal.Cast<byte, int>(decoded).ToArray(), Is.EqualTo(kIndexBuffer));
        }

        [Test]
        public void DecodeIndexV1()
        {
            var decoded = MeshOptimizerIndexDecoder.DecodeIndexBuffer(kIndexBufferTricky.Length, sizeof(int), kIndexDataV1);
            Assert.That(MemoryMarshal.Cast<byte, int>(decoded).ToArray(), Is.EqualTo(kIndexBufferTricky));
        }

        [Test]
        public void DecodeVertexV0()
        {
            var decoded = MeshOptimizerVertexDecoder.DecodeVertexBuffer(kVertexBuffer.Length, Marshal.SizeOf<PV>(), kVertexDataV0, useSimd: false);
            Assert.That(MemoryMarshal.Cast<byte, PV>(decoded).ToArray(), Is.EqualTo(kVertexBuffer));
        }

        [Test]
        public void DecodeVertexV0Simd()
        {
            if (!MeshOptimizerVertexDecoder.IsHardwareAccelerated)
            {
                Assert.Ignore("Vector128 is not hardware accelerated.");
            }

            var decoded = MeshOptimizerVertexDecoder.DecodeVertexBuffer(kVertexBuffer.Length, Marshal.SizeOf<PV>(), kVertexDataV0, useSimd: true);
            Assert.That(MemoryMarshal.Cast<byte, PV>(decoded).ToArray(), Is.EqualTo(kVertexBuffer));
        }

        [Test]
        public void DecodeVertexV1()
        {
            var decoded = MeshOptimizerVertexDecoder.DecodeVertexBuffer(kVertexBuffer.Length, Marshal.SizeOf<PV>(), kVertexDataV1, useSimd: false);
            Assert.That(MemoryMarshal.Cast<byte, PV>(decoded).ToArray(), Is.EqualTo(kVertexBuffer));
        }

        [Test]
        public void DecodeVertexV1Simd()
        {
            if (!MeshOptimizerVertexDecoder.IsHardwareAccelerated)
            {
                Assert.Ignore("Vector128 is not hardware accelerated.");
            }

            var decoded = MeshOptimizerVertexDecoder.DecodeVertexBuffer(kVertexBuffer.Length, Marshal.SizeOf<PV>(), kVertexDataV1, useSimd: true);
            Assert.That(MemoryMarshal.Cast<byte, PV>(decoded).ToArray(), Is.EqualTo(kVertexBuffer));
        }

        [Test]
        public void DecodeVertexV1Custom()
        {
            var decoded = MeshOptimizerVertexDecoder.DecodeVertexBuffer(kVertexBuffer.Length, Marshal.SizeOf<PV>(), kVertexDataV1Custom, useSimd: false);
            Assert.That(MemoryMarshal.Cast<byte, PV>(decoded).ToArray(), Is.EqualTo(kVertexBuffer));
        }

        [Test]
        public void DecodeVertexV1CustomSimd()
        {
            if (!MeshOptimizerVertexDecoder.IsHardwareAccelerated)
            {
                Assert.Ignore("Vector128 is not hardware accelerated.");
            }

            var decoded = MeshOptimizerVertexDecoder.DecodeVertexBuffer(kVertexBuffer.Length, Marshal.SizeOf<PV>(), kVertexDataV1Custom, useSimd: true);
            Assert.That(MemoryMarshal.Cast<byte, PV>(decoded).ToArray(), Is.EqualTo(kVertexBuffer));
        }
    }
}
