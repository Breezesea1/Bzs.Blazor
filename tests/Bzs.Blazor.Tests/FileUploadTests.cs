using System.Linq.Expressions;
using System.Globalization;
using Bunit;
using Bunit.JSInterop;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;

namespace Bzs.Blazor.Tests;

public sealed class FileUploadTests
{
    [Fact]
    public void RendersMeaningfulNativeInputAndPreservesAllowedAttributes()
    {
        using var context = CreateContext();
        var model = new UploadModel();
        var editContext = new EditContext(model);
        var additional = new Dictionary<string, object>
        {
            ["data-upload"] = "documents",
            ["aria-describedby"] = "external-help",
            ["accept"] = "text/plain",
            ["multiple"] = "multiple",
            ["type"] = "text",
        };

        var cut = RenderUpload(context, editContext, model, (builder, sequence) =>
        {
            builder.AddAttribute(sequence, nameof(BzsFileUpload.Id), "attachments");
            builder.AddAttribute(sequence + 1, nameof(BzsFileUpload.Name), "profile.attachments");
            builder.AddAttribute(sequence + 2, nameof(BzsFileUpload.Label), "Attachments");
            builder.AddAttribute(sequence + 3, nameof(BzsFileUpload.Description), "PDF documents only.");
            builder.AddAttribute(sequence + 4, nameof(BzsFileUpload.Accept), ".pdf");
            builder.AddAttribute(sequence + 5, nameof(BzsFileUpload.Multiple), false);
            builder.AddAttribute(sequence + 6, nameof(BzsFileUpload.AdditionalAttributes), additional);
        });

        var input = cut.Find("input[type='file']");
        Assert.Equal("attachments", input.Id);
        Assert.Equal("profile.attachments", input.GetAttribute("name"));
        Assert.Equal(".pdf", input.GetAttribute("accept"));
        Assert.False(input.HasAttribute("multiple"));
        Assert.Equal("documents", input.GetAttribute("data-upload"));
        Assert.Equal("external-help attachments-description", input.GetAttribute("aria-describedby"));
        Assert.Equal("attachments", cut.Find("label").GetAttribute("for"));
        Assert.Equal("No files selected.", cut.Find("[role='status']").TextContent);
    }

    [Fact]
    public async Task SelectionUpdatesControlledValueRaisesCallbacksAndNotifiesField()
    {
        using var context = CreateContext();
        var model = new UploadModel();
        var editContext = new EditContext(model);
        var fieldChanges = new List<FieldIdentifier>();
        var selectedCallbacks = new List<IReadOnlyList<IBrowserFile>>();
        editContext.OnFieldChanged += (_, args) => fieldChanges.Add(args.FieldIdentifier);
        var cut = RenderUpload(context, editContext, model, (builder, sequence) =>
        {
            builder.AddAttribute(sequence, nameof(BzsFileUpload.Multiple), true);
            builder.AddAttribute(
                sequence + 1,
                nameof(BzsFileUpload.SelectionChanged),
                EventCallback.Factory.Create<IReadOnlyList<IBrowserFile>>(
                    model,
                    files => selectedCallbacks.Add(files)));
        });
        var files = new IBrowserFile[]
        {
            new TestBrowserFile("roadmap.pdf", 120, "application/pdf"),
            new TestBrowserFile("notes.txt", 80, "text/plain"),
        };

        await RaiseSelectionAsync(cut, files);

        Assert.Equal(files, model.Files);
        Assert.Single(selectedCallbacks);
        Assert.Equal(files, selectedCallbacks[0]);
        Assert.Single(fieldChanges);
        Assert.Equal(nameof(UploadModel.Files), fieldChanges[0].FieldName);
        Assert.True(editContext.IsModified(fieldChanges[0]));
        Assert.All(files.Cast<TestBrowserFile>(), file => Assert.Equal(0, file.OpenReadCount));
        Assert.Contains("roadmap.pdf", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("notes.txt", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CountAndSizeFailuresAreAddedToTheEditContext()
    {
        using var context = CreateContext();
        var model = new UploadModel();
        var editContext = new EditContext(model);
        var field = editContext.Field(nameof(UploadModel.Files));
        var cut = RenderUpload(context, editContext, model, (builder, sequence) =>
        {
            builder.AddAttribute(sequence, nameof(BzsFileUpload.Multiple), true);
            builder.AddAttribute(sequence + 1, nameof(BzsFileUpload.MaximumFileCount), 2);
            builder.AddAttribute(sequence + 2, nameof(BzsFileUpload.MaximumFileSize), 100L);
            builder.AddAttribute(sequence + 3, nameof(BzsFileUpload.MaximumTotalSize), 150L);
            builder.AddAttribute(sequence + 4, nameof(BzsFileUpload.MaximumFileCountValidationMessage), "At most {0} files.");
            builder.AddAttribute(sequence + 5, nameof(BzsFileUpload.MaximumFileSizeValidationMessage), "Each file is limited to {0} bytes.");
            builder.AddAttribute(sequence + 6, nameof(BzsFileUpload.MaximumTotalSizeValidationMessage), "All files are limited to {0} bytes.");
        });
        var files = new IBrowserFile[]
        {
            new TestBrowserFile("one.bin", 110),
            new TestBrowserFile("two.bin", 40),
            new TestBrowserFile("three.bin", 10),
        };

        await RaiseSelectionAsync(cut, files);

        Assert.Equal(files, model.Files);
        var messages = editContext.GetValidationMessages(field).ToArray();
        Assert.Equal(3, messages.Length);
        Assert.Contains("At most 2 files.", messages);
        Assert.Contains("Each file is limited to 100 bytes.", messages);
        Assert.Contains("All files are limited to 150 bytes.", messages);
        Assert.Equal("true", cut.Find("input[type='file']").GetAttribute("aria-invalid"));
        Assert.Contains("At most 2 files.", cut.Find("[role='alert']").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RemoveAndClearProduceOneValueCallbackAndFieldNotificationPerCommand()
    {
        using var context = CreateContext();
        var first = new TestBrowserFile("first.txt", 10);
        var second = new TestBrowserFile("second.txt", 20);
        var model = new UploadModel { Files = new IBrowserFile[] { first, second } };
        var editContext = new EditContext(model);
        var valueCallbacks = 0;
        var fieldNotifications = 0;
        IBrowserFile? removed = null;
        var cleared = 0;
        editContext.OnFieldChanged += (_, _) => fieldNotifications++;
        var cut = RenderUpload(
            context,
            editContext,
            model,
            (builder, sequence) =>
            {
                builder.AddAttribute(sequence, nameof(BzsFileUpload.Multiple), true);
                builder.AddAttribute(
                    sequence + 1,
                    nameof(BzsFileUpload.FileRemoved),
                    EventCallback.Factory.Create<IBrowserFile>(model, file => removed = file));
                builder.AddAttribute(
                    sequence + 2,
                    nameof(BzsFileUpload.Cleared),
                    EventCallback.Factory.Create(model, () => cleared++));
            },
            _ => valueCallbacks++);
        var inputBeforeRemoval = cut.FindComponent<InputFile>().Instance;

        await cut.FindAll("button").First(button => button.TextContent == "Remove").ClickAsync(new());

        Assert.Same(first, removed);
        Assert.Single(model.Files);
        Assert.Same(second, model.Files[0]);
        Assert.Equal(1, valueCallbacks);
        Assert.Equal(1, fieldNotifications);
        Assert.NotSame(inputBeforeRemoval, cut.FindComponent<InputFile>().Instance);

        await cut.FindAll("button").Single(button => button.TextContent == "Clear files").ClickAsync(new());

        Assert.Empty(model.Files);
        Assert.Equal(2, valueCallbacks);
        Assert.Equal(2, fieldNotifications);
        Assert.Equal(1, cleared);
        Assert.Contains("No files selected.", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ClearingARequiredSelectionAddsARequiredFieldError()
    {
        using var context = CreateContext();
        var model = new UploadModel { Files = new IBrowserFile[] { new TestBrowserFile("required.txt", 10) } };
        var editContext = new EditContext(model);
        var field = editContext.Field(nameof(UploadModel.Files));
        var cut = RenderUpload(context, editContext, model, (builder, sequence) =>
        {
            builder.AddAttribute(sequence, nameof(BzsFileUpload.Required), true);
            builder.AddAttribute(sequence + 1, nameof(BzsFileUpload.RequiredValidationMessage), "A document is required.");
        });

        await cut.FindAll("button").Single(button => button.TextContent == "Clear files").ClickAsync(new());

        Assert.Equal(new[] { "A document is required." }, editContext.GetValidationMessages(field));
        Assert.Equal("A document is required.", cut.Find("[role='alert']").TextContent);
    }

    [Fact]
    public async Task EditContextValidationChecksAnUntouchedRequiredSelection()
    {
        using var context = CreateContext();
        var model = new UploadModel();
        var editContext = new EditContext(model);
        var field = editContext.Field(nameof(UploadModel.Files));
        var cut = RenderUpload(context, editContext, model, (builder, sequence) =>
        {
            builder.AddAttribute(sequence, nameof(BzsFileUpload.Required), true);
            builder.AddAttribute(sequence + 1, nameof(BzsFileUpload.RequiredValidationMessage), "Choose a file.");
        });

        var isValid = await cut.InvokeAsync(editContext.Validate);

        Assert.False(isValid);
        Assert.Equal(new[] { "Choose a file." }, editContext.GetValidationMessages(field));
        Assert.Equal("Choose a file.", cut.Find("[role='alert']").TextContent);
    }

    [Fact]
    public void TemplatesAndConsumerOwnedProgressRenderWithoutReadingFiles()
    {
        using var context = CreateContext();
        var file = new TestBrowserFile("report.csv", 64, "text/csv");
        var model = new UploadModel { Files = new IBrowserFile[] { file } };
        var editContext = new EditContext(model);
        var progress = new Dictionary<IBrowserFile, double?> { [file] = 37.5 };
        var cut = RenderUpload(context, editContext, model, (builder, sequence) =>
        {
            builder.AddAttribute(
                sequence,
                nameof(BzsFileUpload.FileTemplate),
                (RenderFragment<IBrowserFile>)(item => content => content.AddContent(0, $"Template: {item.Name}")));
            builder.AddAttribute(sequence + 1, nameof(BzsFileUpload.Progress), progress);
            builder.AddAttribute(sequence + 2, nameof(BzsFileUpload.ProgressLabel), "Transfer");
        });

        Assert.Contains("Template: report.csv", cut.Markup, StringComparison.Ordinal);
        var progressElement = cut.Find("progress");
        Assert.Equal("37.5", progressElement.GetAttribute("value"));
        Assert.Equal("Transfer: report.csv", progressElement.GetAttribute("aria-label"));
        Assert.Equal(0, file.OpenReadCount);
    }

    [Fact]
    public void EmptyTemplateReplacesTheDefaultEmptyState()
    {
        using var context = CreateContext();
        var model = new UploadModel();
        var editContext = new EditContext(model);
        var cut = RenderUpload(context, editContext, model, (builder, sequence) =>
            builder.AddAttribute(
                sequence,
                nameof(BzsFileUpload.EmptyTemplate),
                (RenderFragment)(content => content.AddContent(0, "Choose supporting evidence."))));

        Assert.Contains("Choose supporting evidence.", cut.Markup, StringComparison.Ordinal);
        Assert.Empty(cut.FindAll("[role='status']"));
    }

    [Fact]
    public async Task DisabledInputIgnoresSyntheticSelectionAndHidesCommands()
    {
        using var context = CreateContext();
        var initial = new TestBrowserFile("existing.txt", 10);
        var model = new UploadModel { Files = new IBrowserFile[] { initial } };
        var editContext = new EditContext(model);
        var cut = RenderUpload(context, editContext, model, (builder, sequence) =>
            builder.AddAttribute(sequence, nameof(BzsFileUpload.Disabled), true));

        await RaiseSelectionAsync(cut, new TestBrowserFile("replacement.txt", 10));

        Assert.Single(model.Files);
        Assert.Same(initial, model.Files[0]);
        Assert.True(cut.Find("input[type='file']").HasAttribute("disabled"));
        Assert.Empty(cut.FindAll("button"));
    }

    [Fact]
    public void DefaultUploadTextIsLocalizedInZhHans()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        var originalUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("zh-Hans");
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("zh-Hans");
            using var context = CreateContext();
            var model = new UploadModel();
            var editContext = new EditContext(model);
            var cut = RenderUpload(context, editContext, model);

            Assert.Equal("未选择文件。", cut.Find("[role='status']").TextContent);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
        }
    }

    private static BunitContext CreateContext()
    {
        var context = new BunitContext();
        context.Services.AddBzsBlazor();
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        return context;
    }

    private static IRenderedComponent<EditForm> RenderUpload(
        BunitContext context,
        EditContext editContext,
        UploadModel model,
        Action<RenderTreeBuilder, int>? addAttributes = null,
        Action<IReadOnlyList<IBrowserFile>>? onValueChanged = null)
    {
        RenderFragment childContent = builder =>
        {
            builder.OpenComponent<BzsFileUpload>(0);
            builder.AddAttribute(1, nameof(BzsFileUpload.Value), model.Files);
            builder.AddAttribute(
                2,
                nameof(BzsFileUpload.ValueChanged),
                EventCallback.Factory.Create<IReadOnlyList<IBrowserFile>>(
                    model,
                    files =>
                    {
                        model.Files = files;
                        onValueChanged?.Invoke(files);
                    }));
            builder.AddAttribute(
                3,
                nameof(BzsFileUpload.ValueExpression),
                (Expression<Func<IReadOnlyList<IBrowserFile>>>)(() => model.Files));
            addAttributes?.Invoke(builder, 4);
            builder.CloseComponent();
        };

        return context.Render<EditForm>(parameters => parameters
            .Add(component => component.EditContext, editContext)
            .Add(component => component.ChildContent, (RenderFragment<EditContext>)(_ => childContent)));
    }

    private static Task RaiseSelectionAsync(
        IRenderedComponent<EditForm> cut,
        params IBrowserFile[] files) =>
        cut.InvokeAsync(() => cut.FindComponent<InputFile>().Instance.OnChange.InvokeAsync(
            new InputFileChangeEventArgs(files)));

    private sealed class UploadModel
    {
        public IReadOnlyList<IBrowserFile> Files { get; set; } = Array.Empty<IBrowserFile>();
    }

    private sealed class TestBrowserFile(
        string name,
        long size,
        string contentType = "application/octet-stream") : IBrowserFile
    {
        public int OpenReadCount { get; private set; }
        public string Name { get; } = name;
        public DateTimeOffset LastModified { get; } = DateTimeOffset.UnixEpoch;
        public long Size { get; } = size;
        public string ContentType { get; } = contentType;

        public Stream OpenReadStream(long maxAllowedSize = 512000, CancellationToken cancellationToken = default)
        {
            OpenReadCount++;
            throw new InvalidOperationException("BzsFileUpload must not open browser file streams.");
        }
    }
}
