using System.Diagnostics;
using WindowsFormsApp1;

namespace EvolandConsoleTestings
{
    // Typing from: https://github.com/HaxeFoundation/hashlink/blob/master/src/hl.h

    public enum hl_type_kind {
        HVOID = 0,
        HUI8 = 1,
        HUI16 = 2,
        HI32 = 3,
        HI64 = 4,
        HF32 = 5,
        HF64 = 6,
        HBOOL = 7,
        HBYTES = 8,
        HDYN = 9,
        HFUN = 10,
        HOBJ = 11,
        HARRAY = 12,
        HTYPE = 13,
        HREF = 14,
        HVIRTUAL = 15,
        HDYNOBJ = 16,
        HABSTRACT = 17,
        HENUM = 18,
        HNULL = 19,
        // ---------
        HLAST = 20,
        _H_FORCE_INT = 0x7FFFFFFF
    }

    public struct hl_field_lookup
    {
        public IntPtr t { get; set; }
        public int hashed_name { get; set; }
        public int field_index { get; set; } // negative or zero : index in methods
    }

    public struct hl_obj_field
    {
        public IntPtr name { get; set; }
        public IntPtr t { get; set; }
        public int hashed_name { get; set; }
    }

    public struct hl_runtime_obj
    {
        public IntPtr t { get; set; }
        // absolute
        public int nfields { get; set; }
        public int nproto { get; set; }
        public int size { get; set; }
        public int nmethods { get; set; }
        public int nbindings { get; set; }
        public bool hasPtr { get; set; }
        public IntPtr methods { get; set; }
        public IntPtr fields_indexes { get; set; }
        public IntPtr bindings { get; set; }
        public IntPtr parent { get; set; }
        public IntPtr toStringFun { get; set; }
        public IntPtr compareFun { get; set; }
        public IntPtr castFun { get; set; }
        public IntPtr getFieldFun { get; set; }
        // relative
        public int nlookup { get; set; }
        public IntPtr lookup { get; set; }
    };
    public struct hl_type_obj {
        public int nfields { get; set; }
        public int nproto { get; set; }
        public int nbindings { get; set; }
        public IntPtr name { get; set; }
        public IntPtr super { get; set; }
        public IntPtr fields { get; set; }
        public IntPtr proto { get; set; }
        public IntPtr bindings { get; set; }
        public IntPtr global_value { get; set; }
        public IntPtr m { get; set; }
        public IntPtr rt { get; set; }
    };

    public struct hl_type_virtual
    {
        public IntPtr fields { get; set; }
        public int nfields { get; set; }
        // runtime
        public int dataSize { get; set; }
        public IntPtr indexes { get; set; }
        public IntPtr lookup { get; set; }
    };

    public struct hl_enum_construct
    {
        public IntPtr name;
        public int nparams;
        public IntPtr paramz;
	    public int size;
        public bool hasptr;
        public IntPtr offsets;
    };

    public struct hl_type_enum
    {

        public IntPtr name;
        public int nconstructs;
        public IntPtr constructs;
        public IntPtr global_value;
    };

internal class Program
    {
        private static int[] T_SIZES = new int[] { 0, 1, 2, 4, 8, 4, 8, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 0 };

        static Process? game;

        static HashSet<IntPtr> visited = new HashSet<IntPtr>();

        static void Main(string[] args)
        {
            Process[] matches = Process.GetProcessesByName("Evoland");
            if (matches == null || matches.Length == 0)
            {
                Console.Error.WriteLine("Evoland not found. Is it running?");
                return;
            }
            game = matches[0];

            while (true)
            {
                Console.Write("Enter address (or press <enter> to exit): ");
                string input = Console.ReadLine();
                if (input == null || input == "")
                {
                    break;
                }
                Console.WriteLine("Scanning address: {0}", input);
                visited.Clear();

                IntPtr startAddress = (IntPtr)Convert.ToInt32(input, 16);

                bool tryAgain = false;
                do 
                {
                    hl_type_kind type = ReadTypeKind(startAddress);
                    switch (type)
                    {
                        case hl_type_kind.HOBJ:
                            ReadHOBJ(startAddress, 0);
                            tryAgain = false;
                            break;
                        case hl_type_kind.HVIRTUAL:
                            ReadHVIRTUAL(startAddress, 0);
                            tryAgain = false;
                            break;
                        case hl_type_kind.HENUM:
                            ReadHENUM(startAddress, 0);
                            tryAgain = false;
                            break;
                        default:
                            if (tryAgain)
                            {
                                Console.Error.WriteLine("Unkown type: {0}", type);
                                tryAgain = false;
                            } else
                            {
                                startAddress = new DeepPointer((IntPtr)Convert.ToInt32(input, 16), 0x0).Deref<IntPtr>(game);
                                tryAgain = true;
                            }
                            break;
                    }
                } while (tryAgain);

                Console.WriteLine();
            }
        }

        static hl_type_kind ReadTypeKind(IntPtr baseAddress)
        {
            if (game == null) { return hl_type_kind.HVOID; }

            return new DeepPointer(baseAddress, 0x0).Deref<hl_type_kind>(game);
        }

        static int ReadTypeInt(IntPtr baseAddress)
        {
            if (game == null) { return 0; }

            return new DeepPointer(baseAddress, 0x0).Deref<int>(game);
        }
        static void ReadHENUM(IntPtr baseAddress, int depth)
        {
            if (game == null) { return; }

            hl_type_enum tenum = new DeepPointer(baseAddress, 0x4, 0x0).Deref<hl_type_enum>(game);

            Console.ForegroundColor = ConsoleColor.Green;
            Enumerable.Range(0, depth).ToList().ForEach(i => Console.Write("\t"));
            List<string> names = new List<string>();
            for (int i = 0; i < tenum.nconstructs; i++)
            {
                hl_enum_construct construct = new DeepPointer(tenum.constructs, 0x0 + 0x18 * i).Deref<hl_enum_construct>(game);
                string enumName = new DeepPointer(construct.name, 0x0).DerefString(game, ReadStringType.UTF16, 50);
                names.Add(enumName);
            }
            Console.WriteLine(String.Join(", ", names));
            Console.ForegroundColor = ConsoleColor.White;
        }

        static void ReadHVIRTUAL(IntPtr baseAddress, int depth)
        {
            if (game == null) { return; }

            if (visited.Contains(baseAddress)) { return; }
            visited.Add(baseAddress);

            hl_type_virtual virt = new DeepPointer(baseAddress, 0x4, 0x0).Deref<hl_type_virtual>(game);

            int start = 0;

            //Dictionary<int, int> lookup = new Dictionary<int, int>();

            //for (int i = 0; i < virt.nfields; i++)
            //{
            //    hl_field_lookup fieldLookup = new DeepPointer(virt.lookup, 0x0 + 0xC * i).Deref<hl_field_lookup>(game);
            //    lookup.Add(fieldLookup.hashed_name, fieldLookup.field_index);
            //}

            for (int i = 0; i < virt.nfields; i++)
            {
                hl_obj_field objField = new DeepPointer(virt.fields, 0x0 + 0xC * i).Deref<hl_obj_field>(game);
                string objFieldName = new DeepPointer(objField.name, 0x0).DerefString(game, ReadStringType.UTF16, 50);
                if (objField.hashed_name != 0)
                {
                    start = start + T_SIZES[ReadTypeInt(objField.t)];
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Enumerable.Range(0, depth).ToList().ForEach(i => Console.Write("\t"));
                    Console.WriteLine("name: {0,-25} at: 0x{1,-3:X} type: {2}", objFieldName, start, ReadTypeKind(objField.t));
                    Console.ForegroundColor = ConsoleColor.White;

                    hl_type_kind type = ReadTypeKind(objField.t);
                    switch (type)
                    {
                        case hl_type_kind.HOBJ:
                            ReadHOBJ(objField.t, depth + 1);
                            break;
                        case hl_type_kind.HVIRTUAL:
                            ReadHVIRTUAL(objField.t, depth + 1);
                            break;
                        case hl_type_kind.HENUM:
                            ReadHENUM(objField.t, depth + 1);
                            break;
                    }
                }
            }
        }

        static void ReadHOBJ(IntPtr baseAddress, int depth)
        {
            if (game == null) { return; }

            if (visited.Contains(baseAddress)) { return; }
            visited.Add(baseAddress);

            hl_type_obj obj = new DeepPointer(baseAddress, 0x4, 0x0).Deref<hl_type_obj>(game);

            if (obj.super != IntPtr.Zero)
            {
                ReadHOBJ(obj.super, depth);
                Console.WriteLine();
            }

            string name = new DeepPointer(obj.name, 0x0).DerefString(game, ReadStringType.UTF16, 50);
            Console.ForegroundColor = ConsoleColor.Magenta;
            Enumerable.Range(0, depth).ToList().ForEach(i => Console.Write("\t"));
            Console.WriteLine("HOBJ name: " + name);
            Console.ForegroundColor = ConsoleColor.White;

            hl_runtime_obj runtimeObj = new DeepPointer(obj.rt, 0x0).Deref<hl_runtime_obj>(game);

            Dictionary<int, int> lookup = new Dictionary<int, int>();

            for (int i = 0; i < runtimeObj.nlookup; i++)
            {
                hl_field_lookup fieldLookup = new DeepPointer(runtimeObj.lookup, 0x0 + 0xC * i).Deref<hl_field_lookup>(game);
                lookup.Add(fieldLookup.hashed_name, fieldLookup.field_index);
            }

            for (int i = 0; i < obj.nfields; i++)
            {
                hl_obj_field objField = new DeepPointer(obj.fields, 0x0 + 0xC * i).Deref<hl_obj_field>(game);
                string objFieldName = new DeepPointer(objField.name, 0x0).DerefString(game, ReadStringType.UTF16, 50);

                if (objField.hashed_name != 0) 
                {
                    Enumerable.Range(0, depth).ToList().ForEach(i => Console.Write("\t"));
                    Console.WriteLine("name: {0,-25} at: 0x{1,-3:X} type: {2}", objFieldName, lookup[objField.hashed_name], ReadTypeKind(objField.t));

                    hl_type_kind type = ReadTypeKind(objField.t);
                    switch (type)
                    {
                        case hl_type_kind.HOBJ:
                            ReadHOBJ(objField.t, depth + 1);
                            break;
                        case hl_type_kind.HVIRTUAL:
                            ReadHVIRTUAL(objField.t, depth + 1);
                            break;
                        case hl_type_kind.HENUM:
                            ReadHENUM(objField.t, depth + 1);
                            break;
                    }
                }
            }
        }
    }
}