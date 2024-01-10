using System.Diagnostics;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Runtime.InteropServices;

public unsafe class Program
{
    static void Main(string[] args)
    {
        if(args.Length!= 1)
        {
            Console.WriteLine("Pass input as argument");
            return;
        }

        var filePath = args[0];
        new Program().Run(filePath);
    }

    private void Run(string filePath)
    {
        var sw = new Stopwatch();
        sw.Start();

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

            workers.Add(new Worker(pointer, start, end));
            //workers.Last().ProcessChunk();
        }
        workers.ForEach(w => w.Start());
        workers.ForEach(w => w.Wait());

        var final = workers[0];

        foreach (var worker in workers.Skip(1))
        {
            foreach(var item in worker.Measurements)
            {
                ref var measurement = ref CollectionsMarshal.GetValueRefOrAddDefault(final.Measurements, item.Key, out bool exist);
                measurement.Merge(item.Value);
            }
        }
        
        Console.Write("{");
        Console.Write(string.Join(", ", final.Measurements
            .Select(m => (City: m.Key.ToString(), Measurement: m.Value))
            .OrderBy(m => m.City, StringComparer.Ordinal).Select(m => $"{m.City}={m.Measurement}")));

        Console.WriteLine("}");

        sw.Stop();
        Console.WriteLine(sw.Elapsed);
    }
}