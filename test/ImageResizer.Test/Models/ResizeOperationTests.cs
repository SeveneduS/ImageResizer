using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Media.Imaging;
using ImageResizer.Properties;
using ImageResizer.Test;
using Xunit;

namespace ImageResizer.Models
{
    public class ResizeOperationTests : IDisposable
    {
        readonly TestDirectory _directory = new TestDirectory();

        [Fact]
        public void Execute_copies_frame_metadata()
        {
            var operation = new ResizeOperation("Test.jpg", _directory, Settings());

            operation.Execute();

            AssertEx.Image(
                _directory.File(),
                image => Assert.Equal("Brice Lambson", ((BitmapMetadata)image.Frames[0].Metadata).Author[0]));
        }

        [Fact]
        public void Execute_keeps_date_modified()
        {
            var operation = new ResizeOperation("Test.jpg", _directory, Settings(s => s.KeepDateModified = true));

            operation.Execute();

            Assert.Equal(File.GetLastWriteTimeUtc("Test.jpg"), File.GetLastWriteTimeUtc(_directory.File()));
        }

        [Fact]
        public void Execute_replaces_originals()
        {
            var path = Path.Combine(_directory, "Test.jpg");
            File.Copy("Test.jpg", path);

            var operation = new ResizeOperation(path, null, Settings(s => s.Replace = true));

            operation.Execute();

            AssertEx.Image(_directory.File(), image => Assert.Equal(96, image.Frames[0].PixelWidth));
        }

        [Fact]
        public void Execute_uniquifies_output_filename()
        {
            File.WriteAllBytes(Path.Combine(_directory, "Test (Test).jpg"), new byte[0]);

            var operation = new ResizeOperation("Test.jpg", _directory, Settings());

            operation.Execute();

            Assert.Contains("Test (Test) (1).jpg", _directory.FileNames);
        }

        [Fact]
        public void Execute_uniquifies_output_filename_again()
        {
            File.WriteAllBytes(Path.Combine(_directory, "Test (Test).jpg"), new byte[0]);
            File.WriteAllBytes(Path.Combine(_directory, "Test (Test) (1).jpg"), new byte[0]);

            var operation = new ResizeOperation("Test.jpg", _directory, Settings());

            operation.Execute();

            Assert.Contains("Test (Test) (2).jpg", _directory.FileNames);
        }

        [Fact]
        public void Execute_uses_fileName_format()
        {
            var operation = new ResizeOperation(
                "Test.jpg",
                _directory,
                Settings(s => s.FileName = "%1_%2_%3_%4_%5_%6"));

            operation.Execute();

            Assert.Contains("Test_Test_96_96_96_96.jpg", _directory.FileNames);
        }

        [Fact]
        public void Execute_transforms_each_frame()
        {
            var operation = new ResizeOperation("Test1.gif", _directory, Settings());

            operation.Execute();

            AssertEx.Image(
                _directory.File(),
                image =>
                {
                    Assert.Equal(2, image.Frames.Count);
                    AssertEx.All(image.Frames, frame => Assert.Equal(96, frame.PixelWidth));
                });
        }

        [Fact]
        public void Execute_uses_fallback_encoder()
        {
            var operation = new ResizeOperation(
                "Test.ico",
                _directory,
                Settings(s => s.FallbackEncoder = new TiffBitmapEncoder().CodecInfo.ContainerFormat));

            operation.Execute();

            Assert.Contains("Test (Test).tiff", _directory.FileNames);
        }

        [Fact]
        public void Execute_ignores_orientation()
        {
            var operation = new ResizeOperation(
                "Test2.png",
                _directory,
                Settings(
                    x =>
                    {
                        x.IgnoreOrientation = true;
                        x.SelectedSize.Width = 96;
                        x.SelectedSize.Height = 192;
                    }));

            operation.Execute();

            AssertEx.Image(
                _directory.File(),
                image =>
                {
                    Assert.Equal(192, image.Frames[0].PixelWidth);
                    Assert.Equal(96, image.Frames[0].PixelHeight);
                });
        }

        [Fact]
        public void Execute_honors_shrink_only()
        {
            var operation = new ResizeOperation(
                "Test2.png",
                _directory,
                Settings(
                    x =>
                    {
                        x.ShrinkOnly = true;
                        x.SelectedSize.Width = 288;
                        x.SelectedSize.Height = 288;
                    }));

            operation.Execute();

            AssertEx.Image(
                _directory.File(),
                image =>
                {
                    Assert.Equal(192, image.Frames[0].PixelWidth);
                    Assert.Equal(96, image.Frames[0].PixelHeight);
                });
        }

        [Fact]
        public void Execute_honors_unit()
        {
            var operation = new ResizeOperation(
                "Test.jpg",
                _directory,
                Settings(
                    x =>
                    {
                        x.SelectedSize.Width = 1;
                        x.SelectedSize.Height = 1;
                        x.SelectedSize.Unit = ResizeUnit.Inch;
                    }));

            operation.Execute();

            AssertEx.Image(_directory.File(), image => Assert.Equal(image.Frames[0].DpiX, image.Frames[0].PixelWidth));
        }

        [Fact]
        public void Execute_honors_fit_when_Fit()
        {
            var operation = new ResizeOperation(
                "Test2.png",
                _directory,
                Settings(x => x.SelectedSize.Fit = ResizeFit.Fit));

            operation.Execute();

            AssertEx.Image(
                _directory.File(),
                image =>
                {
                    Assert.Equal(96, image.Frames[0].PixelWidth);
                    Assert.Equal(48, image.Frames[0].PixelHeight);
                });
        }

        [Fact]
        public void Execute_honors_fit_when_Fill()
        {
            var operation = new ResizeOperation(
                "Test2.png",
                _directory,
                Settings(x => x.SelectedSize.Fit = ResizeFit.Fill));

            operation.Execute();

            AssertEx.Image(
                _directory.File(),
                image =>
                {
                    // TODO: Assert cropped
                    Assert.Equal(96, image.Frames[0].PixelWidth);
                    Assert.Equal(96, image.Frames[0].PixelHeight);
                });
        }

        [Fact]
        public void Execute_honors_fit_when_Stretch()
        {
            var operation = new ResizeOperation(
                "Test2.png",
                _directory,
                Settings(x => x.SelectedSize.Fit = ResizeFit.Stretch));

            operation.Execute();

            AssertEx.Image(
                _directory.File(),
                image =>
                {
                    // TODO: Assert stretched
                    Assert.Equal(96, image.Frames[0].PixelWidth);
                    Assert.Equal(96, image.Frames[0].PixelHeight);
                });
        }

        public void Dispose()
            => _directory.Dispose();

        Settings Settings(Action<Settings> action = null)
        {
            var settings = new Settings
            {
                Sizes = new ObservableCollection<ResizeSize>
                {
                    new ResizeSize
                    {
                        Name = "Test",
                        Width = 96,
                        Height = 96
                    }
                },
                SelectedSizeIndex = 0
            };
            action?.Invoke(settings);

            return settings;
        }
    }
}
