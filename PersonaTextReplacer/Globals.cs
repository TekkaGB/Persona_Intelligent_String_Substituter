using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace PersonaTextReplacer
{
    public static class Globals
    {
        public static string AssemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        public static char s = Path.DirectorySeparatorChar;
        public static string asc = $"{AssemblyLocation}{s}Dependencies{s}AtlusScriptTools{s}AtlusScriptCompiler.exe";
        public static string pe = $"{AssemblyLocation}{s}Dependencies{s}PersonaEditor{s}PersonaEditorCMD.exe";
        public static string pmse = $"{AssemblyLocation}{s}Dependencies{s}PM1MessageScriptEditor{s}PM1MessageScriptEditor.exe";
        public static Logger logger;
    }
}
