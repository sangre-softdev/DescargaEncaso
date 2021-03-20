using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace EnCasoShared
{
    /// <summary>
	/// Define an API for loading and saving a text file. Reference this interface
	/// in the common code, and implement this interface in the app projects for
	/// iOS, Android and WinPhone. Remember to use the 
	///     [assembly: Dependency (typeof (SaveAndLoad_IMPLEMENTATION_CLASSNAME))]
	/// attribute on each of the implementations.
	/// </summary>
	public interface ISaveAndLoadFiles
    {
        string SaveByteAsync(string filename, byte[] file);
        byte[] LoadByte(string filename);
        Task<byte[]> LoadByteAsync(string filename);
        bool FileExists(string filename);
    }
}
