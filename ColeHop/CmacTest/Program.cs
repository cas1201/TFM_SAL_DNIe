using Org.BouncyCastle.Crypto.Macs;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Engines;

// Shared secret from last log
var sharedSecret = Convert.FromHexString("2B062C50E9CAA4EA645FE3E6A1EE6B1FAE95D6341ED43C090B932DA997697630");

Console.WriteLine("=== SHA-256 KDF ===");
TestKdf(sharedSecret, true);

Console.WriteLine("\n=== SHA-1 KDF ===");
TestKdf(sharedSecret, false);

// Chip PK from log  
var chipPk = Convert.FromHexString("0437C76FA80337A8C3C2BDC851FBC9C5B15DA3D05E21F240825180FBE769EC9CF662E11C8FE93E01AF946E8F453B5C781A3DD670FCC6BE61B23BBB9F1E712EEAF7");
var authToken = BuildAuthToken(chipPk);
Console.WriteLine($"\nAuth token ({authToken.Length} bytes): {Convert.ToHexString(authToken)}");

// K_mac from SHA-1 log
var kMacSha1 = Convert.FromHexString("EFAE5F2EA11C0E69EE6FFAEDD547C6DE");
Console.WriteLine($"CMAC with SHA-1 K_mac: {Convert.ToHexString(ComputeCmac(kMacSha1, authToken))}");
Console.WriteLine("Expected from log:     9D625729453AD287");

static void TestKdf(byte[] ss, bool sha256)
{
    var kEnc = Kdf(ss, 1, sha256);
    var kMac = Kdf(ss, 2, sha256);
    Console.WriteLine($"K_enc: {Convert.ToHexString(kEnc)}");
    Console.WriteLine($"K_mac: {Convert.ToHexString(kMac)}");
}

static byte[] Kdf(byte[] ss, int c, bool sha256)
{
    Org.BouncyCastle.Crypto.IDigest d = sha256 ? new Org.BouncyCastle.Crypto.Digests.Sha256Digest() : (Org.BouncyCastle.Crypto.IDigest)new Org.BouncyCastle.Crypto.Digests.Sha1Digest();
    d.BlockUpdate(ss, 0, ss.Length);
    d.BlockUpdate(new byte[] { (byte)(c >> 24), (byte)(c >> 16), (byte)(c >> 8), (byte)c }, 0, 4);
    var o = new byte[d.GetDigestSize()];
    d.DoFinal(o, 0);
    return o.Take(16).ToArray();
}

static byte[] BuildAuthToken(byte[] pk)
{
    var oid = new byte[] { 0x04, 0x00, 0x7F, 0x00, 0x07, 0x02, 0x02, 0x04, 0x02, 0x02 };
    var r = new List<byte> { 0x7F, 0x49 };
    var inner = new List<byte> { 0x06, (byte)oid.Length };
    inner.AddRange(oid);
    inner.Add(0x86); inner.Add((byte)pk.Length); inner.AddRange(pk);
    r.Add((byte)inner.Count); r.AddRange(inner);
    return r.ToArray();
}

static byte[] ComputeCmac(byte[] key, byte[] data)
{
    var m = new CMac(new AesEngine(), 128);
    m.Init(new KeyParameter(key));
    m.BlockUpdate(data, 0, data.Length);
    var o = new byte[m.GetMacSize()];
    m.DoFinal(o, 0);
    return o.Take(8).ToArray();
}
