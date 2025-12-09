using System;

namespace NLogViewer.ClientApplication.Models
{
    /// <summary>
    /// Represents application information extracted from log events
    /// </summary>
    public class AppInfo
    {
        /// <summary>
        /// Application name
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Sender/Remote endpoint information
        /// </summary>
        public string Sender { get; set; } = string.Empty;

        /// <summary>
        /// Unique identifier combining Name and Sender
        /// </summary>
        public string Id => $"{Name};{Sender}";

        public override string ToString()
        {
            return string.IsNullOrEmpty(Sender) ? Name : $"{Name} ({Sender})";
        }

        public override bool Equals(object? obj)
        {
            if (obj is AppInfo other)
            {
                return Id.Equals(other.Id, StringComparison.OrdinalIgnoreCase);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode(StringComparison.OrdinalIgnoreCase);
        }
    }
}

