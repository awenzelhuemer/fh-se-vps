using System;
using System.Collections.Generic;
using System.Drawing;

namespace VPS.Wator.Improved3
{
    // initial object-oriented implementation of the Wator world simulation
    public class Improved3WatorWorld : IWatorWorld
    {
        private Random random;

        // A matrix of ints that determines the order of execution for each cell of the world.
        // This matrix is shuffled in each time step.
        // Cells of the world must be executed in a random order,
        // otherwise the animal in the first cell is always allowed to move first.
        private int[] randomMatrix;

        // for visualization
        private byte[] rgbValues;

        // neighbour points
        private readonly IList<Point> points = new List<Point>();

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

        public Improved3WatorWorld(Settings settings)
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

            // populate the random matrix that determines the order of execution for the cells
            randomMatrix = GenerateRandomMatrix(Width, Height);

            // Initialize the population by placing the required number of shark and fish
            // randomly on the grid.
            // randomMatrix contains all values from 0 .. Width*Height in a random ordering
            // so we can simply place a fish onto a cell if the value in the same cell of the
            // randomMatrix is smaller then the number of fish 
            // subsequently we can place a shark if the number in randomMatrix is smaller than
            // the number of fish and shark
            for (int col = 0; col < Width; col++)
            {
                for (int row = 0; row < Height; row++)
                {
                    int value = randomMatrix[GetGridIndex(row, col)];
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

        // Execute one time step of the simulation. Each cell of the world must be executed once.
        // Animals move around on the grid. To make sure each animal is executed only once we
        // use the moved flag.
        public void ExecuteStep()
        {
            RandomizeMatrix(randomMatrix);  // make sure that order of execution of cells is different and random in each time step

            // process all animals in random order
            int random_row, random_col;
            for (int col = 0; col < Width; col++)
            {
                for (int row = 0; row < Height; row++)
                {
                    // get random position (row/colum) from random matrix
                    random_col = randomMatrix[GetGridIndex(row, col)] % Width;
                    random_row = randomMatrix[GetGridIndex(row, col)] / Width;

                    var animal = Grid[GetGridIndex(random_row, random_col)];

                    if (animal != null && !animal.Moved)  // process unmoved animals
                        animal.ExecuteStep();
                }
            }

            // commit all animals in the grid to prepare for the next simulation step
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
            points.Clear();
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
        private int[] GenerateRandomMatrix(int width, int height)
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
            RandomizeMatrix(matrix);  // shuffle
            return matrix;
        }
        private void RandomizeMatrix(int[] array)
        {
            // perform Knuth shuffle (http://en.wikipedia.org/wiki/Fisher%E2%80%93Yates_shuffle)
            int size = array.Length;
            for (int i = 0; i < (size - 2); i++)
            {
                int result = random.Next(i, size);
                int temp = array[result];
                array[result] = array[i];
                array[i] = temp;
            }
        }
    }
}
