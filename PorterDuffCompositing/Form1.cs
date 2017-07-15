using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PorterDuffCompositing
{
	public partial class Form1 : Form
	{
		public Form1()
		{
			InitializeComponent();

			this.Width = 900;
			this.Height = 700;

			var backgroundPath = @"background.png";
			var foregroundPath = @"foreground.png";

			this.background = MakeBitmap(backgroundPath, null);
			this.foreground = MakeBitmap(foregroundPath, background.Size);

			Debug.Assert(this.background.Size.Equals(this.foreground.Size));

			//SetAlpha(this.background, 120);

			foreach (var prop in typeof(PorterDuffCompositingMode).GetProperties(BindingFlags.Public | BindingFlags.Static).Where(x => x.PropertyType == typeof(PorterDuffCompositingMode)))
			{
				var mode = (PorterDuffCompositingMode)prop.GetValue(null);
				var item = new CompositingModeComboBoxItem(prop.Name, mode);
				this.comboBoxSelectedMode.Items.Add(item);
			}

			this.comboBoxSelectedMode.SelectedIndex = 0;
		}

		private class CompositingModeComboBoxItem
		{
			public CompositingModeComboBoxItem(string name, PorterDuffCompositingMode mode)
			{
				this.Name = name;
				this.Mode = mode;
			}

			public string Name { get; }
			public PorterDuffCompositingMode Mode { get; }

			public override string ToString()
			{
				return this.Name;
			}
		}

		//private static void SetAlpha(Bitmap bitmap, int alpha)
		//{
		//	for (int y = 0; y < bitmap.Height; ++y)
		//	{
		//		for (int x = 0; x < bitmap.Width; ++x)
		//		{
		//			var tmp = bitmap.GetPixel(x, y);
		//			tmp = Color.FromArgb((int)(alpha * tmp.A / 255.0), tmp.R, tmp.G, tmp.B);
		//			bitmap.SetPixel(x, y, tmp);
		//		}
		//	}
		//}

		private static Bitmap MakeBitmap(string file, Size? size)
		{
			using (var tmp = Image.FromFile(file))
			{
				return ReallyCloneImage(tmp, size);
			}
		}

		private static Bitmap ReallyCloneImage(Image img, Size? size)
		{
			var usedSize = size ?? new Size(img.Width, img.Height);
			var clone = new Bitmap(usedSize.Width, usedSize.Height);
			using (var g = Graphics.FromImage(clone))
			{
				g.DrawImage(img, 0, 0, img.Width, img.Height);
			}
			return clone;
		}

		private readonly Bitmap background;
		private readonly Bitmap foreground;
		private Bitmap compositeImage;

		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);

			var img = this.compositeImage;
			if (img != null)
			{
				e.Graphics.DrawImage(img, 0, 0, img.Width, img.Height);
			}
		}

		private void comboBoxSelectedMode_SelectedIndexChanged(object sender, EventArgs e)
		{
			var item = this.comboBoxSelectedMode.SelectedItem as CompositingModeComboBoxItem;
			if (item != null)
			{
				var mode = item.Mode;
				this.compositeImage?.Dispose();
				this.compositeImage = null;
				var sw = Stopwatch.StartNew();
				this.compositeImage = ReallyCloneImage(this.background, null);
				Debug.WriteLine($"Image cloned ({sw.ElapsedMilliseconds} ms)");
				sw.Restart();
				PorterDuffCompositing.Compose(mode, this.foreground, this.compositeImage);
				var elapsed = sw.ElapsedMilliseconds;
				Debug.WriteLine($"Image composed ({elapsed} ms)");

				using (var g = Graphics.FromImage(this.compositeImage))
				{
					var tmp = g.MeasureString(elapsed.ToString(), this.Font);
					g.FillRectangle(Brushes.White, new RectangleF(0, 0, tmp.Width, tmp.Height));
					g.DrawString(elapsed.ToString(), this.Font, Brushes.Black, new Point());
				}

				this.Invalidate();
			}
		}
	}

	public static class PorterDuffCompositing
	{
		public static void Compose(IPorterDuffCompositingMode mode, Bitmap source, Bitmap destination)
		{
			var minWidth = Math.Min(source.Width, destination.Width);
			var minHeight = Math.Min(source.Height, destination.Height);

#if false
			for (int y = 0; y < minHeight; ++y)
			{
				for (int x = 0; x < minWidth; ++x)
				{
					var tmp = mode.ComposePixel(source.GetPixel(x, y), destination.GetPixel(x, y));
					destination.SetPixel(x, y, tmp);
				}
			}
#else
			var rect = new Rectangle(0, 0, minWidth, minHeight);
			using (var sourceData = PixelData.LockBits(source, rect, ImageLockMode.ReadOnly))
			using (var destinationData = PixelData.LockBits(destination, rect, ImageLockMode.ReadWrite))
			{
				for (int y = 0; y < minHeight; ++y)
				{
					var destYOffset = y * destinationData.Stride;
					var destXOffset = y * sourceData.Stride;

					for (int x = 0; x < minWidth; ++x)
					{
						int destIndex = destYOffset + x;
						int srcIndex = destXOffset + x;

						Color destColor = Color.FromArgb(destinationData.Bytes[destIndex]);
						Color srcColor = Color.FromArgb(sourceData.Bytes[srcIndex]);

						destinationData.Bytes[destIndex] = mode.ComposePixel(srcColor, destColor).ToArgb();
					}
				}

				destinationData.Commit();
			}
#endif
		}

		private sealed class PixelData : IDisposable
		{
			private PixelData(Bitmap image, BitmapData bitmapData)
			{
				this.image = image;
				this.bitmapData = bitmapData;
				this.Stride = Math.Abs(bitmapData.Stride) / 4;
			}

			private readonly Bitmap image;
			private readonly BitmapData bitmapData;
			public int[] Bytes { get; private set; }
			public int Stride { get; }

			public static PixelData LockBits(Bitmap image, Rectangle rect, ImageLockMode flags)
			{
				var format = PixelFormat.Format32bppArgb;
				if (image.PixelFormat != format)
				{
					throw new Exception();
				}
				var bitmapData = image.LockBits(rect, flags, format);
				var data = new PixelData(image, bitmapData);
				try
				{
					data.Initialize();
				}
				catch
				{
					data.Dispose();
					throw;
				}
				return data;
			}

			private void Initialize()
			{
				this.Bytes = new int[this.bitmapData.Height * this.Stride];
				Marshal.Copy(this.bitmapData.Scan0, this.Bytes, 0, this.Bytes.Length);
			}

			public void Commit()
			{
				Marshal.Copy(this.Bytes, 0, this.bitmapData.Scan0, this.Bytes.Length);
			}

			public void Dispose()
			{
				this.image.UnlockBits(this.bitmapData);
			}
		}
	}

	public interface IPorterDuffCompositingMode
	{
		Color ComposePixel(Color source, Color destination);
	}

	public class PorterDuffCompositingMode : IPorterDuffCompositingMode
	{
		public PorterDuffCompositingMode(Func<Color, Color, Color> func)
		{
			this.func = func;
		}

		private readonly Func<Color, Color, Color> func;

		public Color ComposePixel(Color source, Color destination)
		{
			return this.func(source, destination);
		}

		public static Color SourceOverFunc(Color source, Color destination)
		{
			// co = αs x Cs + αb x Cb x (1 – αs)
			// αo = αs + αb x (1 – αs)

			double alpha = source.A + destination.A - source.A * destination.A / 255.0;

			return Color.FromArgb(
				(int)alpha,
				SourceOverRGB(source.A, source.R, destination.A, destination.R, alpha),
				SourceOverRGB(source.A, source.G, destination.A, destination.G, alpha),
				SourceOverRGB(source.A, source.B, destination.A, destination.B, alpha)
				);
		}

		private static int SourceOverRGB(double sa, double SC, double da, double DC, double alpha)
		{
			double color_with_alpha = (sa * SC + da * DC - sa * da / 255.0 * DC);
			double tmp = color_with_alpha / alpha;
			if (Double.IsNaN(tmp) || tmp < 0 || tmp > 255)
			{
				if (alpha < 0.5)
				{
					return 0;
				}
				int j = 5;
			}
			return (int)tmp;
		}

		public static Color SourceInFunc(Color source, Color destination)
		{
			var a = (int)(source.A * destination.A / 255.0);
			return Color.FromArgb(
				a,
				source.R,
				source.G,
				source.B
				);
		}

		public static Color SourceOutFunc(Color source, Color destination)
		{
			var a = (int)(source.A * (255.0 - destination.A) / 255.0);
			return Color.FromArgb(
				a,
				source.R,
				source.G,
				source.B
				);
		}

		private static int Clip(double d)
		{
			return (int)Math.Min(Math.Max(d, 0), 255);
		}

		public static Color SourceAtopFunc(Color source, Color destination)
		{
#if true
			// co = αs x Cs x αb + αb x Cb x (1 – αs)
			// αo = αs x αb + αb x (1 – αs)
			//var sa = source.A / 255.0;
			//var da = destination.A / 255.0;
			var a = destination.A;
			//var blend = source.A / 255.0;
			return Color.FromArgb(
				a,
				//αs x Cs x αb + αb x Cb x (1 – αs)
				SourceAtopRGB(source.A, source.R, destination.A, destination.R, a),
				SourceAtopRGB(source.A, source.G, destination.A, destination.G, a),
				SourceAtopRGB(source.A, source.B, destination.A, destination.B, a)
				//Clip(sa * source.R * da + da * destination.R * (1.0 - sa)),
				//Clip(sa * source.G * da + da * destination.G * (1.0 - sa)),
				//Clip(sa * source.B * da + da * destination.B * (1.0 - sa))
				);
#else
			var a = destination.A;
			var blend = source.A / 255.0;
			return Color.FromArgb(
				Clip(a),
				Clip(destination.R + blend * (source.R - destination.R)),
				Clip(destination.G + blend * (source.G - destination.G)),
				Clip(destination.B + blend * (source.B - destination.B))
				);
#endif
		}

		private static int SourceAtopRGB(double sa, double SC, double da, double DC, double alpha)
		{
			// co = αs x Cs x αb + αb x Cb x (1 – αs)
			//    = sa x SC x da + da x DC - da x DC x sa
			double color_with_alpha = sa * SC * da / 255.0 + da * DC - da * DC * sa / 255.0;
			double tmp = color_with_alpha / alpha;
			if (Double.IsNaN(tmp) || tmp < 0 || tmp > 255)
			{
				if (alpha < 0.5)
				{
					return 0;
				}
				int j = 5;
			}
			return (int)tmp;
		}

		private static Random random = new Random();

		public static Color XorFunc(Color source, Color destination)
		{
			// co = αs x Cs x (1 - αb) + αb x Cb x (1 – αs)
			// αo = αs x (1 - αb) + αb x (1 – αs)	 
			var sa = source.A / 255.0;
			var da = destination.A / 255.0;
			var blend = destination.A / 255.0;

			var a = (source.A * (255.0 - destination.A) + destination.A * (255.0 - source.A)) / 255.0;

			return Color.FromArgb(
				(int)a,
				XorRGB(source.A, source.R, destination.A, destination.R, a),
				XorRGB(source.A, source.G, destination.A, destination.G, a),
				XorRGB(source.A, source.B, destination.A, destination.B, a)
				);

			//return Color.FromArgb(
			//	Clip(a),
			//	Clip(sa * source.R * (1.0 - da) + da * destination.R - (1.0 - sa)),
			//	Clip(sa * source.G * (1.0 - da) + da * destination.G - (1.0 - sa)),
			//	Clip(sa * source.B * (1.0 - da) + da * destination.B - (1.0 - sa))
			//	);
			//return Color.FromArgb(
			//	(int)a,
			//	Clip(source.R + blend * (destination.R - source.R)),
			//	Clip(source.G + blend * (destination.G - source.G)),
			//	Clip(source.B + blend * (destination.B - source.B))
			//	);
		}

		private static int XorRGB(double sa, double SC, double da, double DC, double alpha)
		{
			// co = αs x Cs x (1 - αb) + αb x Cb x (1 – αs)
			//    = sa * SC - sa*da*SC + da * DC - sa*da*DC
			
			double color_with_alpha = sa * SC - sa * da * SC / 255.0 + da * DC - sa * da * DC / 255.0;
			double tmp = color_with_alpha / alpha;
			if (Double.IsNaN(tmp) || tmp < 0 || tmp > 255)
			{
				if (alpha < 0.5)
				{
					return 0;
				}
				int j = 5;
			}
			return (int)tmp;
		}

		public static PorterDuffCompositingMode Clear { get; } = new PorterDuffCompositingMode((s, d) => Color.Empty);
		public static PorterDuffCompositingMode SourceOnly { get; } = new PorterDuffCompositingMode((s, d) => s);
		public static PorterDuffCompositingMode DestinationOnly { get; } = new PorterDuffCompositingMode((s, d) => d);
		public static PorterDuffCompositingMode SourceOver { get; } = new PorterDuffCompositingMode(SourceOverFunc);
		public static PorterDuffCompositingMode DestinationOver { get; } = new PorterDuffCompositingMode((s, d) => SourceOverFunc(d, s));
		public static PorterDuffCompositingMode SourceIn { get; } = new PorterDuffCompositingMode(SourceInFunc);
		public static PorterDuffCompositingMode DestinationIn { get; } = new PorterDuffCompositingMode((s, d) => SourceInFunc(d, s));
		public static PorterDuffCompositingMode SourceOut { get; } = new PorterDuffCompositingMode(SourceOutFunc);
		public static PorterDuffCompositingMode DestinationOut { get; } = new PorterDuffCompositingMode((s, d) => SourceOutFunc(d, s));
		public static PorterDuffCompositingMode SourceAtop { get; } = new PorterDuffCompositingMode(SourceAtopFunc);
		public static PorterDuffCompositingMode DestinationAtop { get; } = new PorterDuffCompositingMode((s, d) => SourceAtopFunc(d, s));
		public static PorterDuffCompositingMode Xor { get; } = new PorterDuffCompositingMode(XorFunc);
	}
}
