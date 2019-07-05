using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO.Compression;

public class CompressionUtilities {

    public static byte[] Compress(byte[] data)
    {
        MemoryStream output = new MemoryStream();
        using (DeflateStream dstream = new DeflateStream(output, CompressionMode.Compress))
        {
            dstream.Write(data, 0, data.Length);
        }
        return output.ToArray();
    }

    public static byte[] Decompress(byte[] data)
    {
        MemoryStream input = new MemoryStream(data);
        MemoryStream output = new MemoryStream();
        using (DeflateStream dstream = new DeflateStream(input, CompressionMode.Decompress))
        {
            byte[] deflatedArray = new byte[input.Length];
            dstream.Read(deflatedArray, 0, deflatedArray.Length);
        }
        return output.ToArray();
    }

}
