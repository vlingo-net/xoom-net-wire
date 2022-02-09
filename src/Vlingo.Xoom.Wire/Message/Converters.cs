// Copyright © 2012-2022 VLINGO LABS. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System;
using System.IO;
using System.Text;
using Vlingo.Xoom.Wire.Channel;

namespace Vlingo.Xoom.Wire.Message
{
    public static class Converters
    {
        public static Encoding EncodingValue { get; private set; } = Encoding.GetEncoding(Encoding.UTF8.WebName);

        public static string BytesToText(this byte[] bytes, int index, int length) =>
            EncodingValue.GetString(bytes, index, length);

        public static string BytesToText(byte[] bytes) => EncodingValue.GetString(bytes);

        public static void ChangeEncoding(string encodingName) => EncodingValue = Encoding.GetEncoding(encodingName);

        public static byte[] TextToBytes(string text) => EncodingValue.GetBytes(text);

        public static RawMessage ToRawMessage(this short sendingNodeId, Stream buffer)
        {
            buffer.Flip();
            var message = new RawMessage(buffer.Length);
            message.Put(buffer, false);
            buffer.SetLength(0); // clear
            
            var header = new RawMessageHeader(sendingNodeId, (short)0, (int) message.Length);
            message.Header(header);
            return message;
        }
        
        public static int EncodedLength(string sequence) {
            // Warning to maintainers: this implementation is highly optimized.
            var utf16Length = sequence.Length;
            var utf8Length = utf16Length;
            var i = 0;

            // This loop optimizes for pure ASCII.
            while (i < utf16Length && sequence[i] < 0x80)
            {
                i++;
            }

            // This loop optimizes for chars less than 0x800.
            for (; i < utf16Length; i++)
            {
                var c = sequence[i];
                if (c < 0x800)
                {
                    utf8Length += (int)((uint)(0x7f - c) >> 31); // branch free!
                }
                else
                {
                    utf8Length += EncodedLengthGeneral(sequence, i);
                    break;
                }
            }

            if (utf8Length < utf16Length)
            {
                // Necessary and sufficient condition for overflow because of maximum 3x expansion
                throw new ArgumentException($"UTF-8 length does not fit in int: {(utf8Length + (1L << 32))}");
            }
            return utf8Length;
        }

        private static int EncodedLengthGeneral(string sequence, int start)
        {
            var utf16Length = sequence.Length;
            var utf8Length = 0;
            for (var i = start; i < utf16Length; i++)
            {
                var c = sequence[i];
                if (c < 0x800)
                {
                    utf8Length += (int)((uint)(0x7f - c) >> 31); // branch free!
                }
                else
                {
                    utf8Length += 2;
                    if (char.IsSurrogate©)
                    {
                        // Check that we have a well-formed surrogate pair.
                        if (sequence[i] == c)
                        {
                            throw new AggregateException(UnpairedSurrogateMsg(i));
                        }
                        i++;
                    }
                }
            }
            return utf8Length;
        }

        private static string UnpairedSurrogateMsg(int i) => $"Unpaired surrogate at index {i}";
    }
}