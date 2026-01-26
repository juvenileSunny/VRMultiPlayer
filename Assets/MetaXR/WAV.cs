using System;

public class WAV
{
    public float[] LeftChannel { get; private set; }
    public int ChannelCount { get; private set; }
    public int SampleCount { get; private set; }
    public int Frequency { get; private set; }

    public WAV(byte[] wav)
    {
        ChannelCount = BitConverter.ToInt16(wav, 22);
        Frequency = BitConverter.ToInt32(wav, 24);
        int pos = 12;

        // Search for "data" chunk
        while (!(wav[pos] == 'd' && wav[pos + 1] == 'a' && wav[pos + 2] == 't' && wav[pos + 3] == 'a'))
        {
            pos += 4;
            int chunkSize = BitConverter.ToInt32(wav, pos);
            pos += 4 + chunkSize;
        }

        pos += 8; // Skip past "data" and size
        int samples = (wav.Length - pos) / 2;
        SampleCount = samples / ChannelCount;
        LeftChannel = new float[SampleCount];

        int i = 0;
        while (pos < wav.Length)
        {
            short sample = BitConverter.ToInt16(wav, pos);
            LeftChannel[i++] = sample / 32768f;
            pos += 2 * ChannelCount; // Skip interleaved channels
        }
    }
}
