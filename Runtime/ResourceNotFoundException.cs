using System;

namespace Radish.AssetManagement
{
    public class ResourceNotFoundException : Exception
    {
        public ResourceNotFoundException(string guid, Type type) : base($"{type.FullName} not found with guid '{guid}'")
        {
        }
    }
}