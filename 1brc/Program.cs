using System.Diagnostics;
using System.IO.MemoryMappedFiles;

public unsafe class Program
{
    static void Main(string[] args)
    {
        if(args.Length < 1)
        {
            Console.WriteLine("Pass input as argument.");
            return;
        }

        var filePath = args[0];
        var isClassic = args.Length > 1 && args[1] == "/classic";
        new Program().Run(filePath, isClassic);
    }

    private void Run(string filePath, bool isClassic)
    {
        var length = new FileInfo(filePath).Length;
        var chunkCount = length > 10000 ? Environment.ProcessorCount : 1;
        var chunkSize = length / chunkCount;

        var memoryMappedFile = MemoryMappedFile.CreateFromFile(filePath, FileMode.Open, null, 0, MemoryMappedFileAccess.Read);
        var viewAccessor = memoryMappedFile.CreateViewAccessor(0, length, MemoryMappedFileAccess.Read);
        var safeViewHandle = viewAccessor.SafeMemoryMappedViewHandle;

        var pointer = (byte*)0;

        safeViewHandle.AcquirePointer(ref pointer);

        var workers = new List<Worker>(chunkCount);

        for (int idx = 0; idx < chunkCount; idx++)
        {
            var start = chunkSize * idx;
            var end = start + chunkSize - 1;

            if(idx == chunkCount - 1)
                end = length - 1;

            workers.Add(new Worker(pointer, start, end, isClassic));
#if DEBUG
            workers.Last().ProcessChunk();
#endif
        }

#if !DEBUG
        workers.ForEach(w => w.Start());
        workers.ForEach(w => w.Wait());
#endif
        var final = workers[0];

        foreach (var worker in workers.Skip(1))
        {
            foreach(var item in worker.Measurements)
            {
                ref var measurement = ref final.Measurements.GetValueRefOrAddDefault(item.Key);
                measurement.Merge(item.Value);
            }
        }
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        Console.Write("{");
        Console.Write(string.Join(", ", final.Measurements
            .Select(m => (City: m.Key.ToString(), Measurement: m.Value))
            .OrderBy(m => m.City, StringComparer.Ordinal).Select(m => $"{m.City}={m.Measurement}")));

        Console.WriteLine("}");
    }
}