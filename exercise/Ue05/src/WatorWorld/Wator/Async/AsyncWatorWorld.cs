using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;

namespace VPS.Wator.Async
{
    // initial object-oriented implementation of the Wator world simulation
    public class AsyncWatorWorld : IWatorWorld
    {
        private Random random;

        // for visualization
        private byte[] rgbValues;

        private int[] randomMatrix;

        private readonly OrderablePartitioner<Tuple<int, int>> partitioner;

        #region Properties
        public int Width { get; private set; }  // width (number of cells) of the world
        public int Height { get; private set; }  // height (number of cells) of the world
        public Animal[] Grid { get; private set; }  // the cells of the world (2D-array of animal (fish or shark), empty cells have the value null)

        // simulation parameters
        public int InitialFishPopulation { get; private set; }
        public int InitialFishEnergy { get; private set; }
        public int FishBreedTime { get; private set; }

        public int InitialSharkPopulation { get; private set; }
        public int InitialSharkEnergy { get; private set; }
        public int SharkBreedEnergy { get; private set; }
        #endregion

        public AsyncWatorWorld(Settings settings)
        {
            Width = settings.Width;
            Height = settings.Height;
            InitialFishPopulation = settings.InitialFishPopulation;
            InitialFishEnergy = settings.InitialFishEnergy;
            FishBreedTime = settings.FishBreedTime;
            InitialSharkPopulation = settings.InitialSharkPopulation;
            InitialSharkEnergy = settings.InitialSharkEnergy;
            SharkBreedEnergy = settings.SharkBreedEnergy;

            rgbValues = new byte[Width * Height * 4];

            random = new Random();
            Grid = new Animal[Width * Height];

            // Create partitions with size of 4
            for (int partitionSize = 4; partitionSize < Height; partitionSize++)
            {
                partitioner = Partitioner.Create(0, Height, partitionSize);
                int partitionCount = partitioner.GetDynamicPartitions().ToList().Count();

                // Count of partitions has to be even otherwise there would be a race condition
                if (partitionCount % 2 == 0)
                {
                    break;
                }
            }

            // Don't randomize matrix initially
            randomMatrix = GenerateMatrix(Width, Height, randomize: false);

            // Randomize positions
            var initialMatrix = GenerateMatrix(Width, Height);
            for (int col = 0; col < Width; col++)
            {
                for (int row = 0; row < Height; row++)
                {
                    int value = initialMatrix[GetGridIndex(row, col)];
                    if (value < InitialFishPopulation)
                    {
                        Grid[GetGridIndex(row, col)] = new Fish(this, new Point(col, row), random.Next(0, FishBreedTime));
                    }
                    else if (value < InitialFishPopulation + InitialSharkPopulation)
                    {
                        Grid[GetGridIndex(row, col)] = new Shark(this, new Point(col, row), random.Next(0, SharkBreedEnergy));
                    }
                    else
                    {
                        Grid[GetGridIndex(row, col)] = null;
                    }
                }
            }
        }

        public int GetGridIndex(int row, int column)
        {
            return row * Width + column;
        }

        public void ExecuteStep()
        {
            Task.Run(ExecuteStepAsync).Wait();
        }

        private Task ExecutePartitionAsync(int lowerHeight, int upperHeight)
        {
            return Task.Run(() =>
            {
                RandomizeMatrix(randomMatrix, lowerHeight, upperHeight);  // make sure that order of execution of cells is different and random in each time step

                // process all animals in random order
                int randomRow, randomCol;
                for (int col = 0; col < Width; col++)
                {
                    // Only execute steps in the given bounds
                    for (int row = lowerHeight; row < upperHeight; row++)
                    {
                        // get random position (row/colum) from random matrix
                        randomCol = randomMatrix[GetGridIndex(row, col)] % Width;
                        randomRow = randomMatrix[GetGridIndex(row, col)] / Width;

                        var animal = Grid[GetGridIndex(randomRow, randomCol)];

                        if (animal != null && !animal.Moved)  // process unmoved animals
                            animal.ExecuteStep();
                    }
                }
            });
        }

        // Execute one time step of the simulation. Each cell of the world must be executed once.
        // Animals move around on the grid. To make sure each animal is executed only once we
        // use the moved flag.
        private async Task ExecuteStepAsync()
        {
            var partitions = partitioner.GetDynamicPartitions().ToList();
            ICollection<Task> tasks = new List<Task>();

            // Execute even partitions
            for (int i = 0; i < partitions.Count; i += 2)
            {
                tasks.Add(ExecutePartitionAsync(partitions[i].Item1, partitions[i].Item2));
            }

            await Task.WhenAll(tasks);
            tasks.Clear();

            // Execute uneven partitions
            for (int i = 1; i < partitions.Count; i += 2)
            {
                tasks.Add(ExecutePartitionAsync(partitions[i].Item1, partitions[i].Item2));
            }

            await Task.WhenAll(tasks);

            // Commit all animals in the grid to prepare for the next simulation step
            for (int col = 0; col < Width; col++)
            {
                for (int row = 0; row < Height; row++)
                {
                    Grid[GetGridIndex(row, col)]?.Commit();
                }
            }
        }

        // generate bitmap for the current state of the Wator world
        public Bitmap GenerateImage()
        {
            int counter = 0;
            for (int row = 0; row < Height; row++)
            {
                for (int col = 0; col < Width; col++)
                {
                    Color color;
                    if (Grid[GetGridIndex(row, col)] == null) color = Color.DarkBlue;
                    else color = Grid[GetGridIndex(row, col)].Color;

                    rgbValues[counter++] = color.B; // blue
                    rgbValues[counter++] = color.G; // green
                    rgbValues[counter++] = color.R; // red
                    rgbValues[counter++] = color.A; // alpha
                }
            }

            Rectangle rect = new Rectangle(0, 0, Width, Height);
            var bitmap = new Bitmap(Width, Height);
            System.Drawing.Imaging.BitmapData bmpData = null;
            try
            {
                // lock the bitmap's bits
                bmpData = bitmap.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadWrite, bitmap.PixelFormat);

                // get the address of the first line
                IntPtr ptr = bmpData.Scan0;

                // copy RGB values back to the bitmap
                System.Runtime.InteropServices.Marshal.Copy(rgbValues, 0, ptr, rgbValues.Length);
            }
            finally
            {
                // unlock the bits
                if (bmpData != null) bitmap.UnlockBits(bmpData);
            }
            return bitmap;
        }

        // find all neighboring cells of the given position and type
        public IList<Point> GetNeighbors(Type type, Point position)
        {
            // neighbour points
            IList<Point> points = new List<Point>();
            int i, j;

            // look north
            i = position.X;
            j = (position.Y + Height - 1) % Height;
            var animal = Grid[GetGridIndex(j, i)];
            if (type == null && animal == null)
            {
                points.Add(new Point(i, j));
            }
            else if (type != null && type.IsInstanceOfType(animal))
            {
                if (animal != null && !animal.Moved)
                {  // ignore animals moved in the current iteration
                    points.Add(new Point(i, j));
                }
            }
            // look east
            i = (position.X + 1) % Width;
            j = position.Y;
            animal = Grid[GetGridIndex(j, i)];
            if (type == null && animal == null)
            {
                points.Add(new Point(i, j));
            }
            else if (type != null && type.IsInstanceOfType(animal))
            {
                if (animal != null && !animal.Moved)
                {
                    points.Add(new Point(i, j));
                }
            }
            // look south
            i = position.X;
            j = (position.Y + 1) % Height;
            animal = Grid[GetGridIndex(j, i)];
            if (type == null && animal == null)
            {
                points.Add(new Point(i, j));
            }
            else if (type != null && type.IsInstanceOfType(animal))
            {
                if (animal != null && !animal.Moved)
                {
                    points.Add(new Point(i, j));
                }
            }
            // look west
            i = (position.X + Width - 1) % Width;
            j = position.Y;
            animal = Grid[GetGridIndex(j, i)];
            if (type == null && animal == null)
            {
                points.Add(new Point(i, j));
            }
            else if (type != null && type.IsInstanceOfType(animal))
            {
                if (animal != null && !animal.Moved)
                {
                    points.Add(new Point(i, j));
                }
            }

            return points;
        }

        // select a random neighboring cell of the given position and type
        public Point SelectNeighbor(Type type, Point position)
        {
            IList<Point> neighbors = GetNeighbors(type, position);  // find all neighbors of required type
            if (neighbors.Count > 1)
            {
                return neighbors[random.Next(neighbors.Count)];  // return random neighbor (prevent bias)
            }
            else if (neighbors.Count == 1)
            {  // only one neighbor -> return without calling random
                return neighbors[0];
            }
            else
            {
                return new Point(-1, -1);  // no neighbor found
            }
        }

        // create a matrix containing all numbers from 0 to width * height in random order
        private int[] GenerateMatrix(int width, int height, bool randomize = true)
        {
            int[] matrix = new int[width * height];

            int row = 0;
            int col = 0;
            for (int i = 0; i < matrix.Length; i++)
            {
                matrix[GetGridIndex(row, col)] = i;
                col++;
                if (col >= width) { col = 0; row++; }
            }
            if (randomize)
            {
                RandomizeMatrix(matrix, 0, height);  // shuffle
            }

            return matrix;
        }

        private void RandomizeMatrix(int[] array, int lowerHeight, int upperHeight)
        {
            // perform Knuth shuffle (http://en.wikipedia.org/wiki/Fisher%E2%80%93Yates_shuffle)
            for (int i = lowerHeight * Width; i < upperHeight * Width; i++)
            {
                int result = random.Next(i, Width * upperHeight);
                int temp = array[result];
                array[result] = array[i];
                array[i] = temp;
            }
        }
    }
}
