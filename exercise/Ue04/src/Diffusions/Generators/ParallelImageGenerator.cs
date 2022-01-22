using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Diffusions.Generators
{
    public class ParallelImageGenerator : ImageGenerator
    {
        protected override void UpdateMatrix(Area area)
        {
            lock (area.Matrix)
            {
                var m = area.Matrix;
                var partitioner = Partitioner.Create(0, area.Width);

                Parallel.ForEach(partitioner, (partRange, _) =>
                {
                    for (int x = partRange.Item1; x < partRange.Item2; x++)
                    {
                        int pX = (x + area.Width - 1) % area.Width;
                        int nX = (x + 1) % area.Width;

                        for (int y = 0; y < area.Height; y++)
                        {
                            int pY = (y + area.Height - 1) % area.Height;
                            int nY = (y + 1) % area.Height;
                            area.NextMatrix[x, y] = (
                            m[pX, pY] + m[pX, y] + m[pX, nY] +
                            m[x, pY] + m[x, nY] +
                            m[nX, pY] + m[nX, y] + m[nX, nY]) / 8;
                        }
                    }
                });
                var tmp = area.NextMatrix;
                area.NextMatrix = area.Matrix;
                area.Matrix = tmp;
            }
        }
    }
}
