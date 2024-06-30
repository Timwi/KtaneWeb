using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RT.Servers;
using RT.Util.ExtensionMethods;

namespace KtaneWeb
{
    partial class KtanePropellerModule
    {
        private Dictionary<string, string> getOpenGraphData(KtaneModuleInfo info)
        {
            return new()
            {
                ["title"] = info.Name,
                ["type"] = "website",
                ["image"] = $"https://ktane.timwi.de/Icons/{(info.HasIcon(_config) ? info.FileName ?? info.Name : "blank")}.png",
                ["url"] = $"https://ktane.timwi.de/HTML/{info.FileName ?? info.Name}.html",
                ["description"] = info.Descriptions.FirstOrDefault(d => d.Language == "English")?.Description ?? "Error: indescribable item"
            };
        }

        private HttpResponse injectOpenGraphData(HttpRequest req, HttpResponse resp)
        {
            if (!resp.Headers.ContentType?.EqualsIgnoreCase("text/html; charset=utf-8") ?? true
                || resp.Status != HttpStatusCode._200_OK
                || req.Method != HttpMethod.Get)
                return resp;

            // Only modify manual page HTML
            if (!req.Url.Path.UrlUnescape().RegexMatch(@"^/HTML/([^/]+\.html)$", out var match))
                return resp;

            if (!_moduleInfoCache.SheetToJsonLookup.TryGetValue(match.Groups[1].Value, out var info))
                return resp;

            var openGraphData = getOpenGraphData(info);
            if (openGraphData.Count == 0)
                return resp;

            return new HttpResponseContent(HttpStatusCode._200_OK, resp.Headers, () => createOpenGraphStream(resp, openGraphData));
        }

        private Stream createOpenGraphStream(HttpResponse resp, Dictionary<string, string> data)
        {
            var innerStream = resp.GetContentStream();

            // The opening <head> tag should be within the leading 64 bytes of the file.
            var header = innerStream.Read(64);
            var ix = header.IndexOfSubarray("<head>".ToUtf8());
            if (ix == -1)
                return new HeaderStream(header, innerStream);

            var ogBlock = data.Select(kvp => $"\n    <meta property=\"og:{kvp.Key}\" content=\"{kvp.Value.HtmlEscape()}\" />").JoinString().ToUtf8();

            var fullHeader = new byte[header.Length + ogBlock.Length];
            Array.Copy(header, fullHeader, ix + 6);
            Array.Copy(ogBlock, 0, fullHeader, ix + 6, ogBlock.Length);
            Array.Copy(header, ix + 6, fullHeader, ix + 6 + ogBlock.Length, header.Length - ix - 6);

            return new HeaderStream(fullHeader, innerStream);
        }

        private sealed class HeaderStream : Stream
        {
            private readonly byte[] _head;
            private readonly Stream _tail;
            private int _bytesRead = 0;

            public HeaderStream(byte[] head, Stream tail)
            {
                _head = head;
                _tail = tail;
            }

            public override bool CanRead => true;
            public override bool CanSeek => false;
            public override bool CanWrite => false;
            public override long Length => _head.Length + _tail.Length;
            public override long Position
            {
                get => throw new NotSupportedException();
                set => throw new NotSupportedException();
            }

            public override void Flush() => _tail.Flush();

            public override int Read(byte[] buffer, int offset, int count)
            {
                if (_bytesRead == -1)
                    return _tail.Read(buffer, offset, count);

                if (_bytesRead + count > _head.Length)
                {
                    Array.Copy(_head, _bytesRead, buffer, offset, _head.Length - _bytesRead);
                    var moved = _tail.Read(buffer, offset + _head.Length - _bytesRead, count - (_head.Length - _bytesRead));
                    _bytesRead = -1;
                    return moved + _head.Length - _bytesRead;
                }

                Array.Copy(_head, _bytesRead, buffer, offset, count);
                _bytesRead += count;
                if (_bytesRead == _head.Length)
                    _bytesRead = -1;
                return count;
            }

            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

            public override void SetLength(long value) => throw new NotSupportedException();

            public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        }
    }
}
