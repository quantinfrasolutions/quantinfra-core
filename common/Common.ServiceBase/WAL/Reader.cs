using System.Globalization;
using System.Reflection;
using System.Text;
using QuantInfra.Common.Messaging;

namespace QuantInfra.Common.ServiceBase.WAL;

internal sealed class Reader
{
    private const char _delimiter = '|';

    // Map: FullName -> Type (fast lookup)
    private readonly Dictionary<string, Type> _typeByFullName;

    public Reader(params Assembly[] assembliesToScan)
    {
        if (assembliesToScan == null || assembliesToScan.Length == 0)
            assembliesToScan = AppDomain.CurrentDomain.GetAssemblies();

        _typeByFullName = BuildTypeMap(assembliesToScan);
    }

    public IEnumerable<(long receivedAt, ITransportMessage transportMessage)> Read(string path)
    {
        // StreamReader is fine here because the file is line-based UTF-8 text.
        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read,
            bufferSize: 4 * 1024 * 1024, options: FileOptions.SequentialScan);

        using var reader = new StreamReader(fs, Encoding.UTF8, detectEncodingFromByteOrderMarks: true,
            bufferSize: 256 * 1024, leaveOpen: false);

        string? line;
        long lineNo = 0;

        while ((line = reader.ReadLine()) != null)
        {
            lineNo++;
            if (line.Length == 0) continue;

            // Split into 6 parts by first 4 delimiters:
            // receivedAt | typeFullName | senderCompId | messageType | sessionId | seq | payload(with possible delimiters)
            if (!TrySplit5(line, _delimiter, out var receivedAtText, out var typeName, out var sender, out var msgTypeText, out var sessionText, out var seqText, out var payload))
                throw new FormatException($"Bad line format at line {lineNo}.");
            
            if (!long.TryParse(receivedAtText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var receivedAt))
                throw new FormatException($"Invalid ReceivedAt '{receivedAtText}' at line {lineNo}.");

            if (!_typeByFullName.TryGetValue(typeName, out var msgType))
                throw new InvalidOperationException($"Unknown message type '{typeName}' at line {lineNo}. " +
                                                    $"Make sure its assembly is scanned and it implements ITransportMessage.");

            // Require parameterless ctor for speed & simplicity
            if (Activator.CreateInstance(msgType) is not ITransportMessage msg)
                throw new InvalidOperationException($"Type '{msgType.FullName}' could not be created as ITransportMessage at line {lineNo}. " +
                                                    $"Ensure it has a public parameterless constructor.");
            
            if (!Enum.TryParse<MessageType>(msgTypeText, out var messageType))
                throw new FormatException($"Invalid MessageType '{msgTypeText}' at line {lineNo}.");
            

            if (!long.TryParse(sessionText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var sessionId))
                throw new FormatException($"Invalid SequenceNumber '{seqText}' at line {lineNo}.");
            
            if (!long.TryParse(seqText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var seq))
                throw new FormatException($"Invalid SessionId '{seqText}' at line {lineNo}.");
            
            msg.ConstructFromLog(sender, (MessageType)messageType, sessionId, seq, payload);

            yield return (receivedAt, msg);
        }
    }

    private static Dictionary<string, Type> BuildTypeMap(IEnumerable<Assembly> assemblies)
    {
        var map = new Dictionary<string, Type>(StringComparer.Ordinal);

        foreach (var asm in assemblies)
        {
            Type[] types;
            try { types = asm.GetTypes(); }
            catch (ReflectionTypeLoadException ex) { types = ex.Types.Where(t => t != null).Cast<Type>().ToArray(); }

            foreach (var t in types)
            {
                if (t.IsAbstract || t.IsInterface) continue;
                if (!typeof(ITransportMessage).IsAssignableFrom(t)) continue;
                if (t.FullName is null) continue;

                // If duplicates exist across assemblies, you should switch to AssemblyQualifiedName in the log.
                map.TryAdd(t.FullName, t);
            }
        }

        return map;
    }

    // Splits into exactly 5 parts by the first 4 delimiters; payload is the remainder.
    private static bool TrySplit5(string s, char delim,
        out string p0, out string p1, out string p2, out string p3, out string p4, out string p5, out string p6)
    {
        p0 = p1 = p2 = p3 = p4 = p5 = p6 = string.Empty;

        int i0 = s.IndexOf(delim);
        if (i0 < 0) return false;

        int i1 = s.IndexOf(delim, i0 + 1);
        if (i1 < 0) return false;

        int i2 = s.IndexOf(delim, i1 + 1);
        if (i2 < 0) return false;

        int i3 = s.IndexOf(delim, i2 + 1);
        if (i3 < 0) return false;

        int i4 = s.IndexOf(delim, i3 + 1);
        if (i4 < 0) return false;
        
        int i5 = s.IndexOf(delim, i4 + 1);
        if (i5 < 0) return false;

        p0 = s.Substring(0, i0);
        p1 = s.Substring(i0 + 1, i1 - (i0 + 1));
        p2 = s.Substring(i1 + 1, i2 - (i1 + 1));
        p3 = s.Substring(i2 + 1, i3 - (i2 + 1));
        p4 = s.Substring(i3 + 1, i4 - (i3 + 1));
        p5 = s.Substring(i4 + 1, i5 - (i4 + 1));
        p6 = s.Substring(i5 + 1); // rest of line = payload (may contain delimiters)

        return true;
    }
}