using Microsoft.AspNetCore.Mvc;
using System.IO;
using Renci.SshNet;

namespace VideoPlayer.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VideoController : ControllerBase
    {
        private const int BufferSize = 64 * 1024; // 64 KB buffer size for streaming
        private readonly SftpClient _sftpClient;

        public VideoController()
        {
            // Initialize SFTP client in the constructor
            _sftpClient = new SftpClient("localhost", 22, "your-username", "your-password");
            _sftpClient.Connect();
        }

        [HttpGet("stream")]
        public async Task<IActionResult> StreamVideo([FromQuery] string videoFileName)
        {
            if (string.IsNullOrEmpty(videoFileName))
            {
                return BadRequest("Video file name must be provided.");
            }

            string filePath = Path.Combine("wwwroot", "videos", videoFileName);

            if (!System.IO.File.Exists(filePath))
            {
                return NotFound("Video file not found.");
            }

            var fileInfo = new FileInfo(filePath);
            long totalSize = fileInfo.Length;

            // Handle Range Request
            var rangeHeader = Request.Headers["Range"].ToString();
            long start = 0, end = totalSize - 1;

            if (!string.IsNullOrEmpty(rangeHeader))
            {
                var range = rangeHeader.Replace("bytes=", "").Split('-');
                start = long.Parse(range[0]);
                if (range.Length > 1 && !string.IsNullOrEmpty(range[1]))
                {
                    end = long.Parse(range[1]);
                }
            }

            long contentLength = end - start + 1;
            Response.StatusCode = 206; // Partial Content
            Response.Headers.Add("Content-Range", $"bytes {start}-{end}/{totalSize}");
            Response.Headers.Add("Accept-Ranges", "bytes");
            Response.ContentLength = contentLength;

            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                stream.Seek(start, SeekOrigin.Begin);
                var buffer = new byte[BufferSize];
                long remaining = contentLength;

                while (remaining > 0)
                {
                    int read = await stream.ReadAsync(buffer, 0, (int)Math.Min(buffer.Length, remaining));
                    if (read == 0) break;
                    await Response.Body.WriteAsync(buffer, 0, read);
                    remaining -= read;
                }
            }

            return new EmptyResult();
        }

        [HttpGet("streamWithSftp")]
        public async Task<IActionResult> StreamVideoWithSftp([FromQuery] string videoFileName)
        {
            if (string.IsNullOrWhiteSpace(videoFileName))
            {
                return BadRequest("Video file name must be provided.");
            }

            try
            {
                // Connect to the SFTP server
                if (!_sftpClient.IsConnected)
                {
                    _sftpClient.Connect();
                }

                if (!_sftpClient.Exists(videoFileName))
                {
                    return NotFound($"Video file '{videoFileName}' not found on SFTP server.");
                }

                // Retrieve file size
                var fileSize = _sftpClient.GetAttributes(videoFileName).Size;

                // Handle Range Request
                var rangeHeader = Request.Headers["Range"].ToString();
                long start = 0, end = fileSize - 1;

                if (!string.IsNullOrEmpty(rangeHeader) && rangeHeader.StartsWith("bytes="))
                {
                    var range = rangeHeader["bytes=".Length..].Split('-');
                    start = long.Parse(range[0]);

                    if (range.Length > 1 && !string.IsNullOrEmpty(range[1]))
                    {
                        end = long.Parse(range[1]);
                    }
                }

                // Validate range
                if (start >= fileSize || end >= fileSize || start > end)
                {
                    return StatusCode(416, $"Requested Range Not Satisfiable. File size: {fileSize} bytes.");
                }

                long contentLength = end - start + 1;

                // Set Response Headers
                Response.StatusCode = 206; // Partial Content
                Response.Headers.Add("Content-Range", $"bytes {start}-{end}/{fileSize}");
                Response.Headers.Add("Accept-Ranges", "bytes");
                Response.ContentLength = contentLength;

                // Stream the file
                using (var stream = _sftpClient.OpenRead(videoFileName))
                {
                    stream.Seek(start, SeekOrigin.Begin);
                    var buffer = new byte[64 * 1024]; // 64 KB buffer size
                    long remaining = contentLength;

                    while (remaining > 0)
                    {
                        var bytesRead = await stream.ReadAsync(buffer, 0, (int)Math.Min(buffer.Length, remaining));
                        if (bytesRead == 0) break;

                        await Response.Body.WriteAsync(buffer, 0, bytesRead);
                        remaining -= bytesRead;
                    }
                }

                return new EmptyResult();
            }
            catch (FileNotFoundException)
            {
                return NotFound($"Video file '{videoFileName}' not found.");
            }
            catch (UnauthorizedAccessException)
            {
                return StatusCode(403, "Access to the video file is denied.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An unexpected error occurred: {ex.Message}");
            }
            finally
            {
                // Ensure SFTP client is disconnected
                if (_sftpClient.IsConnected)
                {
                    _sftpClient.Disconnect();
                }
            }
        }
    }
}