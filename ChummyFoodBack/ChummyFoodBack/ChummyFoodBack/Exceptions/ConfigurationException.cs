using System;

namespace ChummyFoodBack.Exceptions;

public class ConfigurationException : Exception
{
    public ConfigurationException(string notPassedConnectionStringWithNameDefault): base(notPassedConnectionStringWithNameDefault)
    {
    }
}
