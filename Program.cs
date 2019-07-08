namespace GVMFinder
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using System.Reflection.Metadata;
    using System.Reflection.PortableExecutable;
    using System.Text;

    public static class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine("Usage: GVMFinder PathToDirectoryWithDlls");
                return;
            }

            foreach (var file in Directory.GetFiles(args[0], "*.dll", SearchOption.AllDirectories))
            {
                using (var fs = new FileStream(file, FileMode.Open, FileAccess.Read))
                {
                    using (var pereader = new PEReader(fs))
                    {
                        if (!pereader.HasMetadata)
                        {
                            continue;
                        }

                        var reader = pereader.GetMetadataReader();
                        foreach (var methodDefinitionHandle in reader.MethodDefinitions)
                        {
                            var methodDefinition = reader.GetMethodDefinition(methodDefinitionHandle);
                            if ((methodDefinition.Attributes & MethodAttributes.Virtual) != 0 && (methodDefinition.Attributes & MethodAttributes.NewSlot) != 0)
                            {
                                var genericParameterHandleCollection = methodDefinition.GetGenericParameters();
                                if (genericParameterHandleCollection.Count > 0)
                                {
                                    var sb = new StringBuilder(128);

                                    var stack = new Stack<TypeDefinition>();

                                    var typeDef = reader.GetTypeDefinition(methodDefinition.GetDeclaringType());
                                    stack.Push(typeDef);

                                    while (typeDef.IsNested)
                                    {
                                        typeDef = reader.GetTypeDefinition(typeDef.GetDeclaringType());
                                        stack.Push(typeDef);
                                    }

                                    if (stack.Count > 1)
                                    {
                                        while (stack.Count != 0)
                                        {
                                            var item = stack.Pop();
                                            if (!item.IsNested)
                                            {
                                                sb.Append(reader.GetString(item.Namespace));
                                                sb.Append(".");
                                                sb.Append(reader.GetString(item.Name));
                                                sb.Append("+");
                                            }
                                            else
                                            {
                                                sb.Append(reader.GetString(item.Name));
                                            }
                                        }
                                    }
                                    else
                                    {
                                        var item = stack.Pop();
                                        sb.Append(reader.GetString(item.Namespace));
                                        sb.Append(".");
                                        sb.Append(reader.GetString(item.Name));
                                    }

                                    Console.WriteLine($"{sb}.{reader.GetString(methodDefinition.Name)}");
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}