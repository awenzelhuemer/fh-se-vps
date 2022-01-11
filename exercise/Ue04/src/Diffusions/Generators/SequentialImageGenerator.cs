using System.Threading;

namespace Diffusions.Generators
{
    public class SequentialImageGenerator : ImageGenerator
    {
        protected override void UpdateMatrix(Area area)
        {
            lock (area.Matrix)
            {
                var m = area.Matrix;

                for (int x = 0; x < area.Width; x++)
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
                var tmp = area.NextMatrix;
                area.NextMatrix = area.Matrix;
                area.Matrix = tmp;
            }
        }
    }
}
