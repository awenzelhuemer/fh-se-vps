namespace RaceConditionExample
{
    public class Test
    {
        #region Public Properties

        public int Result { get; private set; } = 0;

        #endregion

        #region Public Methods

        public void Work1()
        { Result = 1; }

        public void Work2()
        { Result = 2; }

        public void Work3()
        { Result = 3; }

        #endregion
    }
}