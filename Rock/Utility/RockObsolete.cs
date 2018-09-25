using System;

namespace Rock
{
    /// <summary>
    /// Marks the version at which an [Obsolete] item became obsolete
    /// </summary>
    /// <seealso cref="System.Attribute" />
    [AttributeUsage( AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Constructor | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Event | AttributeTargets.Interface | AttributeTargets.Delegate, Inherited = false )]
    public class RockObsolete : System.Attribute
    {
<<<<<<< HEAD
        /// <summary>
        /// Gets the version.
        /// </summary>
        /// <value>
        /// The version.
        /// </value>
        public string Version { get; private set; }
=======
        private string _version;
>>>>>>> adaf517094... - Added new [RockObsolete] attribute that will indicate which version a [Obsolete] item was made obsolete

        /// <summary>
        /// Initializes a new instance of the <see cref="RockObsolete"/> class.
        /// </summary>
<<<<<<< HEAD
        /// <param name="version">The version when this became obsolete (for example, "1.7")</param>
        public RockObsolete( string version )
        {
            Version = version;
=======
        /// <param name="version">The version when this became obsolete (for example, "v7")</param>
        public RockObsolete( string version )
        {
            _version = version;
>>>>>>> adaf517094... - Added new [RockObsolete] attribute that will indicate which version a [Obsolete] item was made obsolete
        }
    }
}
