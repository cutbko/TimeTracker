using System;

namespace TimeTracker
{
    public static class Const
    {
        public static string AppDataPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TimeTracker");
    }
}