// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license. 
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using Markdig.Parsers;
using Markdig.Syntax;

namespace Markdig.Renderers.Html
{
    /// <summary>
    /// An HTML renderer for a <see cref="CodeBlock"/> and <see cref="FencedCodeBlock"/>.
    /// </summary>
    /// <seealso cref="Markdig.Renderers.Html.HtmlObjectRenderer{Markdig.Syntax.CodeBlock}" />
    public class CodeBlockRenderer : HtmlObjectRenderer<CodeBlock>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CodeBlockRenderer"/> class.
        /// </summary>
        public CodeBlockRenderer()
        {
            BlocksAsDiv = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        public bool OutputAttributesOnPre { get; set; }

        /// <summary>
        /// Gets a map of fenced code block infos that should be rendered as div blocks instead of pre/code blocks.
        /// </summary>
        public HashSet<string> BlocksAsDiv { get; }

        public string PlantUmlServerUrl { get; set; }


        public static string GetEncodedPlantDiagram(string diagram)
        {
            return encode64(deflateStr(diagram));
        }

        private static byte[] deflateStr(String str)
        {
            using (MemoryStream output = new MemoryStream())
            {
                using (DeflateStream gzip = new DeflateStream(output, CompressionMode.Compress))
                {
                    using (StreamWriter writer = new StreamWriter(gzip, System.Text.Encoding.UTF8))
                    {
                        writer.Write(str);
                    }
                }

                return output.ToArray();
            }
        }


        private static string encode64(byte[] data)
        {
            string r = "";
            for (var i = 0; i < data.Length; i += 3)
            {
                     if (i + 2 == data.Length) { r += append3bytes(data[i], data[i + 1], 0); }
                else if (i + 1 == data.Length) { r += append3bytes(data[i], 0, 0); }
                else                           { r += append3bytes(data[i], data[i + 1], data[i + 2]); }
            }
            return r;
        }

        private static string append3bytes(byte b1, byte b2, byte b3)
        {
            int c1 = b1 >> 2;
            int c2 = ((b1 & 0x3) << 4) | (b2 >> 4);
            int c3 = ((b2 & 0xF) << 2) | (b3 >> 6);
            int c4 = b3 & 0x3F;
            var r = "";
            r += encode6bit((byte)(c1 & 0x3F));
            r += encode6bit((byte)(c2 & 0x3F));
            r += encode6bit((byte)(c3 & 0x3F));
            r += encode6bit((byte)(c4 & 0x3F));
            return r;
        }

        private static char encode6bit(byte b)
        {
            if (b < 10){ return (char)(48 + b);}

            b -= 10;
            if (b < 26){return (char)(65 + b);}

            b -= 26;
            if (b < 26){return (char)(97 + b);}

            b -= 26;
            if (b == 0){return '-';}

            if (b == 1){return '_';}

            return '?';
        }

        protected override void Write(HtmlRenderer renderer, CodeBlock obj)
        {
            renderer.EnsureLine();

            var fencedCodeBlock = obj as FencedCodeBlock;
            if (fencedCodeBlock?.Info != null
                    && (fencedCodeBlock.Info == "plantuml")
                    && ! String.IsNullOrEmpty(PlantUmlServerUrl)
                )
            {

                var diagramStr = obj.Lines.ToString();
                var code = GetEncodedPlantDiagram(diagramStr);
                renderer.WriteLine($"<img src=\"{PlantUmlServerUrl}/png/{code}>\">");
            }
            else if (fencedCodeBlock?.Info != null && BlocksAsDiv.Contains(fencedCodeBlock.Info))
            {
                var infoPrefix = (obj.Parser as FencedCodeBlockParser)?.InfoPrefix ??
                                 FencedCodeBlockParser.DefaultInfoPrefix;

                // We are replacing the HTML attribute `language-mylang` by `mylang` only for a div block
                // NOTE that we are allocating a closure here

                if (renderer.EnableHtmlForBlock)
                {
                    renderer.Write("<div")
                            .WriteAttributes(obj.TryGetAttributes(),
                                cls => cls.StartsWith(infoPrefix) ? cls.Substring(infoPrefix.Length) : cls)
                            .Write(">");
                }

                renderer.WriteLeafRawLines(obj, true, true, true);

                if (renderer.EnableHtmlForBlock)
                {
                    renderer.WriteLine("</div>");
                }

            }
            else
            {
                if (renderer.EnableHtmlForBlock)
                {
                    renderer.Write("<pre");

                    if (OutputAttributesOnPre)
                    {
                        renderer.WriteAttributes(obj);
                    }

                    renderer.Write("><code");

                    if (!OutputAttributesOnPre)
                    {
                        renderer.WriteAttributes(obj);
                    }

                    renderer.Write(">");
                }

                renderer.WriteLeafRawLines(obj, true, true);

                if (renderer.EnableHtmlForBlock)
                {
                    renderer.WriteLine("</code></pre>");
                }
            }
        }
    }
}