using System;
using System.IO;

public static class RecordedMusicLoop
{
    public static void Prepare(string input, string output)
    {
        byte[] pcm = null;
        int channels = 0, rate = 0;
        using (var reader = new BinaryReader(File.OpenRead(input)))
        {
            if (new string(reader.ReadChars(4)) != "RIFF") throw new InvalidDataException("RIFF required");
            reader.ReadInt32(); reader.ReadChars(4);
            while (reader.BaseStream.Position + 8 <= reader.BaseStream.Length)
            {
                string id = new string(reader.ReadChars(4));
                int size = reader.ReadInt32();
                long next = reader.BaseStream.Position + size + (size & 1);
                if (id == "fmt ")
                {
                    if (reader.ReadInt16() != 1) throw new InvalidDataException("PCM required");
                    channels = reader.ReadInt16(); rate = reader.ReadInt32();
                    reader.ReadInt32(); reader.ReadInt16();
                    if (reader.ReadInt16() != 16) throw new InvalidDataException("16 bit required");
                }
                else if (id == "data") pcm = reader.ReadBytes(size);
                reader.BaseStream.Position = next;
            }
        }
        if (pcm == null || channels < 1 || rate < 1) throw new InvalidDataException("Missing audio");
        int frames = pcm.Length / (2 * channels);
        // Commercial masters can end in a long fade and silence. Joining inside that silence
        // leaves an audible dip even when adjacent samples are continuous. Find the last
        // audible 100 ms block before forming the two-second overlap.
        int block = Math.Max(1, rate / 10), lastAudible = frames;
        var levels = new double[(frames + block - 1) / block];
        double loudest = 0;
        for (int b = 0; b < levels.Length; b++)
        {
            int end = Math.Min(frames, (b + 1) * block);
            double energy = 0;
            for (int f = b * block; f < end; f++)
                for (int c = 0; c < channels; c++)
                {
                    double v = BitConverter.ToInt16(pcm, (f * channels + c) * 2) / 32768.0;
                    energy += v * v;
                }
            levels[b] = Math.Sqrt(energy / ((end - b * block) * channels));
            loudest = Math.Max(loudest, levels[b]);
        }
        for (int b = levels.Length - 1; b >= 0; b--)
            if (levels[b] >= loudest * 0.1)
            { lastAudible = Math.Min(frames, (b + 1) * block); break; }
        if (lastAudible > rate * 6) frames = lastAudible;
        int fade = Math.Min(rate * 2, frames / 3), length = frames - fade;
        var samples = new float[length * channels];
        float peak = 0;
        for (int frame = 0; frame < length; frame++)
        {
            double phase = (frame + 0.5) / fade * Math.PI * 0.5;
            for (int channel = 0; channel < channels; channel++)
            {
                int index = frame * channels + channel;
                float sample = BitConverter.ToInt16(pcm, index * 2) / 32768f;
                if (frame < fade)
                {
                    float tail = BitConverter.ToInt16(pcm, ((length + frame) * channels + channel) * 2) / 32768f;
                    sample = (float)(sample * Math.Sin(phase) + tail * Math.Cos(phase));
                }
                samples[index] = sample;
                peak = Math.Max(peak, Math.Abs(sample));
            }
        }
        float gain = peak > 0 ? 0.85f / peak : 1;
        using (var writer = new BinaryWriter(File.Create(output)))
        {
            writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); writer.Write(36 + samples.Length * 2);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt ")); writer.Write(16); writer.Write((short)1);
            writer.Write((short)channels); writer.Write(rate); writer.Write(rate * channels * 2); writer.Write((short)(channels * 2)); writer.Write((short)16);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("data")); writer.Write(samples.Length * 2);
            foreach (float sample in samples) writer.Write((short)Math.Round(Math.Max(-1, Math.Min(1, sample * gain)) * 32767));
        }
    }
}
