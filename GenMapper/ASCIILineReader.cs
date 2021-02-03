using System;
using System.IO;

namespace GenMapper
{
    // Utility class for reading ASCII encoded text from a stream.
    // Parsing ASCII text using ASCIILineReader may offer better performance
    // than parsing text using .NET's StreamReader.

    public class ASCIILineReader
    {
        private const int DEFAULT_BUFFER_SIZE = 4096;

        private readonly Stream _stream;
        private readonly byte[] _byteBuffer;

        private int _byteBufferSize;
        private int _byteBufferPos;

        public ASCIILineReader(Stream stream, int bufferSize = DEFAULT_BUFFER_SIZE)
        {
            if (bufferSize < 1)
                throw new ArgumentException("Buffer size must be greater than zero", "bufferSize");

            if (!stream.CanRead)
                throw new ArgumentException("Stream must have read functionality", "stream");

            _stream = stream;

            _byteBuffer = new byte[bufferSize];
            _byteBufferSize = 0;
            _byteBufferPos = 0;
        }

        // Note that there are no checks that ensure the stream was not modified between calls.
        // There is no surefire way to determine if the stream has been modified or not and moreover
        // there is little reason to do so.

        // Reads as many chars into the span as it can accommodate or until a newline or EOF is encountered, 
        // and returns the number of chars read. Does not consume the newline char(s).
        public int ReadChars(Span<char> buffer)
        {
            int charsRead = 0;
            while (charsRead < buffer.Length)
            {
                if (_byteBufferPos < _byteBufferSize)
                {
                    // Note that this may give us garbage values if the file is not actually 
                    // encoded in ASCII . . .
                    char ch = unchecked((char)_byteBuffer[_byteBufferPos]);

                    if (ch == '\r' || ch == '\n')
                        return charsRead;
                    else
                    {
                        _byteBufferPos++;
                        buffer[charsRead++] = ch;
                    }
                }
                else
                {
                    _byteBufferSize = _stream.Read(_byteBuffer, 0, _byteBuffer.Length);
                    _byteBufferPos = 0;

                    if (_byteBufferSize == 0) // Indicates EOF
                        return charsRead;
                }
            }
            return charsRead;
        }

        // Skips over all consecutive occurences of the specified char, and returns the number of chars skipped.
        public int SkipConsecutive(char ch)
        {
            int occurences = 0;
            do
            {
                while (_byteBufferPos < _byteBufferSize)
                {
                    if (ch == unchecked((char)_byteBuffer[_byteBufferPos]))
                    {
                        occurences++;
                        _byteBufferPos++;
                    }
                    else
                        return occurences;
                }
                _byteBufferSize = _stream.Read(_byteBuffer, 0, _byteBuffer.Length);
                _byteBufferPos = 0;
            }
            while (_byteBufferSize != 0);
            return occurences;
        }

        // Moves to the start of the next line. Returns false if EOF is reached.
        public bool NextLine()
        {
            // Platform-specific newlines: \n - UNIX  \r\n - DOS  \r - Mac
            // Indicates whether the previously read character was '\r' (carriage return).
            bool previousCharCR = false;
            do
            {
                while (_byteBufferPos < _byteBufferSize)
                {
                    if (previousCharCR)
                    {
                        if (unchecked((char)_byteBuffer[_byteBufferPos] == '\n'))
                            _byteBufferPos++;

                        return true;
                    }
                    else
                    {
                        char ch = unchecked((char)_byteBuffer[_byteBufferPos++]);

                        if (ch == '\n')
                            return true;
                        else if (ch == '\r')
                            previousCharCR = true;
                    }
                }
                _byteBufferSize = _stream.Read(_byteBuffer, 0, _byteBuffer.Length);
                _byteBufferPos = 0;
            }
            while (_byteBufferSize != 0);
            return false;
        }
    }
}