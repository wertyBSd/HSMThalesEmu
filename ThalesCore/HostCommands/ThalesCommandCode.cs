using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HostCommands
{
    /// <summary>
    /// The ThalesCommandCode attribute should be attached to all classes that inherit
    /// from <see cref="HostCommands.AHostCommand"/>.
    /// </summary>
    /// <remarks>
    /// The attribute is parsed at runtime and is used by <see cref="ThalesMain"/>
    /// to find classes that implement host commands.
    /// </remarks> 
    [AttributeUsage(AttributeTargets.Class)]
    public class ThalesCommandCode: Attribute
    {
        /// <summary>
        /// Racal Command Code.
        /// </summary>
        /// <remarks>
        /// The command code of the host command implemented by a class.
        /// </remarks>
        public string CommandCode;

        /// <summary>
        /// Racal Response Code.
        /// </summary>
        /// <remarks>
        /// The response code of the host command implemented by a class.
        /// </remarks>
        public string ResponseCode;

        /// <summary>
        /// Racal Response Code after I/O.
        /// </summary>
        /// <remarks>
        /// The response code, after I/O is concluded, of the host command implemented by a class.
        /// </remarks>
        public string ResponseCodeAfterIO;

        /// <summary>
        /// Command description.
        /// </summary>
        /// <remarks>
        /// A description of the host command implemented by a class.
        /// </remarks>
        public string Description;

        public ThalesCommandCode(string commandCode, string responseCode, string responseCodeAfterIO, string Description)
        {
            this.CommandCode = commandCode;
            this.ResponseCode = responseCode;
            this.ResponseCodeAfterIO = responseCodeAfterIO;
            this.Description = Description;
        }
    }
}
