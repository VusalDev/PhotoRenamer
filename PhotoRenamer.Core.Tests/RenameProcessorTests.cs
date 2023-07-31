using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace PhotoRenamer.Core.Tests;

public class RenameProcessorTests
{
    private const string OriginalImgFileName = "IMG_20230528_174703.jpg";
    private const string DesiredImgFileName = "2023-05-28 17-47-03.jpg";

    /// <summary>
    /// ����� � ������������ ������������, ������� ������ �������� ��� �����.
    /// </summary>
    private static readonly string OriginalImgPath = Path.Combine(Directory.GetCurrentDirectory(), "Content");
    private static readonly DateTime DesiredImgDateTime = new(2023, 05, 28, 17, 47, 03, DateTimeKind.Local);

    private readonly ILogger _logger;

    public RenameProcessorTests(ITestOutputHelper testOutput)
    {
        var loggerFactory = new LoggerFactory().AddXUnit(testOutput);
        _logger = loggerFactory.CreateLogger<RenameProcessor>();
    }

    [Fact]
    public async Task RenameImgSuccess()
    {
        var (fileDir, filePath) = CopyImage();

        var processor = new RenameProcessor(_logger);
        await processor.RenameAllAsync(fileDir);

        Assert.False(File.Exists(filePath));
        var newFilePath = Path.Combine(fileDir, DesiredImgFileName);
        Assert.True(File.Exists(newFilePath));

        var createData = File.GetCreationTime(newFilePath);
        Assert.Equal(DesiredImgDateTime, createData);

        Cleanup(fileDir);
    }

    [Fact]
    public async Task RenameImgDuplicateSuccess()
    {
        var (fileDir, filePath) = CopyImage();

        var processor = new RenameProcessor(_logger);
        await processor.RenameAllAsync(fileDir);

        Assert.False(File.Exists(filePath));
        var newFilePath = Path.Combine(fileDir, DesiredImgFileName);
        Assert.True(File.Exists(newFilePath));

        // Create original file again
        File.Copy(newFilePath, filePath);
        await processor.RenameAllAsync(fileDir);

        // assert processed file is alive
        Assert.False(File.Exists(filePath));
        Assert.True(File.Exists(newFilePath));

        // assert conflicting file is created
        var newFilePath2 = Path.Combine(fileDir, DesiredImgFileName.Replace(".jpg", "_0001.jpg"));
        Assert.True(File.Exists(newFilePath2));

        Directory.Delete(fileDir, true);
    }

    /// <summary>
    /// �������� ����� ����� � ������������ ������������.
    /// ��� ��� ����� ����������� ����� �����, ��� ������� ����� ������ ���� ��������.
    /// </summary>
    /// <returns>(DirectoryPath, FileName)</returns>
    private (string, string) CopyImage()
    {
        var originalFilePath = Path.Combine(OriginalImgPath, OriginalImgFileName);
        Assert.True(File.Exists(originalFilePath));

        var tempDirectory = Directory.CreateTempSubdirectory().FullName;

        var newFilePath = Path.Combine(tempDirectory, OriginalImgFileName);
        File.Copy(originalFilePath, newFilePath);

        _logger.LogInformation("Create temp directory {Dir}", tempDirectory);

        return (tempDirectory, newFilePath);
    }

    private void Cleanup(string directory)
    {
        _logger.LogInformation("Delete temp directory {Dir}", directory);
        Directory.Delete(directory, true);
    }
}