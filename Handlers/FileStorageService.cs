using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;

namespace smp_trask1.Handlers
{
    public class FileStorageService 
    {
        private readonly IAmazonS3 _s3Client;
        private readonly string _publicObjectUrl;

        public FileStorageService(IConfiguration configuration)
        {
            var serviceUrl =
                configuration["SupabaseStorage:ServiceUrl"]
                ?? throw new InvalidOperationException(
                    "Supabase ServiceUrl is missing.");

            var region =
                configuration["SupabaseStorage:Region"]
                ?? throw new InvalidOperationException(
                    "Supabase Region is missing.");

            var accessKey =
                configuration["SupabaseStorage:AccessKeyId"]
                ?? throw new InvalidOperationException(
                    "Supabase AccessKeyId is missing.");

            var secretKey =
                configuration["SupabaseStorage:SecretAccessKey"]
                ?? throw new InvalidOperationException(
                    "Supabase SecretAccessKey is missing.");

            _publicObjectUrl =
                configuration["SupabaseStorage:PublicObjectUrl"]
                ?? throw new InvalidOperationException(
                    "Supabase PublicObjectUrl is missing.");

            var credentials = new BasicAWSCredentials(
                accessKey,
                secretKey);

            var s3Configuration = new AmazonS3Config
            {
                ServiceURL = serviceUrl,
                AuthenticationRegion = region,
                ForcePathStyle = true
            };

            _s3Client = new AmazonS3Client(
                credentials,
                s3Configuration);
        }

        public async Task<string> UploadFileAsync(
            IFormFile file,
            string bucketName,
            string folderName)
        {
            ValidateFile(file);

            var extension = Path.GetExtension(file.FileName)
                .ToLowerInvariant();

            var uniqueFileName =
                $"{Guid.NewGuid()}{extension}";

            var cleanFolderName =
                folderName.Trim('/');

            var objectPath =
                $"{cleanFolderName}/{uniqueFileName}";

            await using var fileStream =
                file.OpenReadStream();

            var uploadRequest = new PutObjectRequest
            {
                BucketName = bucketName,
                Key = objectPath,
                InputStream = fileStream,
                ContentType = file.ContentType
            };

            await _s3Client.PutObjectAsync(uploadRequest);

            var publicUrl =
                $"{_publicObjectUrl.TrimEnd('/')}/" +
                $"{bucketName}/{objectPath}";

            return publicUrl;
        }

        private static void ValidateFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                throw new ArgumentException(
                    "Please select a file.");
            }

            const long maximumFileSize =
                5 * 1024 * 1024;

            if (file.Length > maximumFileSize)
            {
                throw new ArgumentException(
                    "File size cannot exceed 5 MB.");
            }

            var extension = Path.GetExtension(file.FileName)
                .ToLowerInvariant();

            string[] allowedExtensions =
            {
                ".jpg",
                ".jpeg",
                ".png",
                ".pdf"
            };

            if (!allowedExtensions.Contains(extension))
            {
                throw new ArgumentException(
                    "Only JPG, JPEG, PNG and PDF files are allowed.");
            }
        }
    }
}