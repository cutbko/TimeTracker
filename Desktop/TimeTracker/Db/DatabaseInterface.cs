using System;
using System.Windows;

namespace TimeTracker.Db
{
    public class DatabaseInterface
    {
        private DataCommand _dataCommand;

        public void Init()
        {
            _dataCommand = new DataCommand();
            if (!_dataCommand.DbExists())
            {
                try
                {
                    _dataCommand.CreateDbFile();

                    _dataCommand.ExecuteAsNonQuery(@"CREATE TABLE 'TimeRecords'
                                                    (
                                                        'Id' VARCHAR(20) PRIMARY KEY NOT NULL  UNIQUE , 
                                                        'CreatedAt' INTEGER NOT NULL, 
                                                        'CreatedAtDate' INTEGER NOT NULL, 
                                                        'TotalMinutes' DOUBLE NOT NULL,
                                                        'IsSynced' INT
                                                    )");

                    var result = _dataCommand.ExecuteScalar<long>("SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='TimeRecords';");
                }
                catch (Exception e)
                {
                    MessageBox.Show("Error: " + e);
                }
            }
        }
    }
}