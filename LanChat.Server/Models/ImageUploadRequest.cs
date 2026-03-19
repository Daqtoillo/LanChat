namespace LanChat.Server.Models
{
    public class ImageUploadRequest
    {
        public IFormFile File {  get; set; }
        public string UploaderName { get; set; }
    }
}
