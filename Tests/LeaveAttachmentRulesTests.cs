using Core.Common;

namespace Tests;

public class LeaveAttachmentRulesTests
{
    private static readonly byte[] Pdf = "%PDF-1.7 ..."u8.ToArray();
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00];
    private static readonly byte[] Jpg = [0xFF, 0xD8, 0xFF, 0xE0, 0x00];

    [Fact]
    public void A_Real_Pdf_Is_Accepted()
    {
        var r = LeaveAttachmentRules.Check("cert.PDF", 1000, Pdf, 0);
        Assert.Null(r.Error);
        Assert.Equal("application/pdf", r.ContentType);
    }

    [Fact] public void A_Real_Png_Is_Accepted() => Assert.Null(LeaveAttachmentRules.Check("a.png", 10, Png, 0).Error);
    [Fact] public void A_Real_Jpeg_Is_Accepted() => Assert.Null(LeaveAttachmentRules.Check("a.jpeg", 10, Jpg, 0).Error);

    [Fact]
    public void A_Renamed_File_Is_Refused() =>
        Assert.Contains("don't match", LeaveAttachmentRules.Check("virus.pdf", 10, "MZ\x90\0"u8, 0).Error);

    [Fact]
    public void A_Pdf_Named_As_A_Png_Is_Refused() =>
        Assert.NotNull(LeaveAttachmentRules.Check("a.png", 10, Pdf, 0).Error);

    [Theory]
    [InlineData("a.docx")]
    [InlineData("a.exe")]
    [InlineData("noextension")]
    public void Other_Types_Are_Refused(string name) =>
        Assert.Contains("Only PDF", LeaveAttachmentRules.Check(name, 10, Pdf, 0).Error);

    [Fact]
    public void Size_Limits_Apply()
    {
        Assert.NotNull(LeaveAttachmentRules.Check("a.pdf", 0, Pdf, 0).Error);
        Assert.Null(LeaveAttachmentRules.Check("a.pdf", LeaveAttachmentRules.MaxBytes, Pdf, 0).Error);
        Assert.NotNull(LeaveAttachmentRules.Check("a.pdf", LeaveAttachmentRules.MaxBytes + 1, Pdf, 0).Error);
    }

    [Fact]
    public void The_Count_Limit_Applies() =>
        Assert.Contains("at most 5", LeaveAttachmentRules.Check("a.pdf", 10, Pdf, 5).Error);
}