namespace Neco.Benchmark;

public class HsvConversion {
	[Benchmark]
	public Hsv Single() => RgbByte.ToHsv2(new RgbByte(127, 128, 123));
	[Benchmark]
	public Hsv Integers() => RgbByte.ToHsv(new RgbByte(127, 128, 123));
}

public readonly record struct RgbByte(Byte Red, Byte Green, Byte Blue) {
	public static Hsv ToHsv(RgbByte rgb) {
		Int32 r = rgb.Red ;
		Int32 g = rgb.Green;
		Int32 b = rgb.Blue;
		Int32 max = Math.Max(Math.Max(r, g), b);
		Int32 min = Math.Min(Math.Min(r, g), b);
		Single delta = max - min;
		Single hue = 0;
		if (delta != 0) {
			if (r == max) {
				hue = (g - b) / delta;
			} else {
				if (g == max) {
					hue = 2 + (b - r) / delta;
				} else {
					hue = 4 + (r - g) / delta;
				}
			}

			hue *= 60;
			if (hue < 0) hue += 360;
		}

		var saturation = max == 0 ? 0 : (max - min) / (Single)max;
		var brightness = max;

		return new Hsv(hue, saturation, brightness/255.0f);
	}
	
	public static Hsv ToHsv2(RgbByte rgb) {
		Single r = rgb.Red / 255.0f;
		Single g = rgb.Green / 255.0f;
		Single b = rgb.Blue / 255.0f;
		Single max = Math.Max(Math.Max(r, g), b);
		Single min = Math.Min(Math.Min(r, g), b);
		Single delta = max - min;
		Single hue = 0;
		if (delta != 0) {
			if (r == max) {
				hue = (g - b) / delta;
			} else {
				if (g == max) {
					hue = 2 + (b - r) / delta;
				} else {
					hue = 4 + (r - g) / delta;
				}
			}

			hue *= 60;
			if (hue < 0) hue += 360;
		}

		var saturation = max == 0 ? 0 : (max - min) / max;
		var brightness = max;

		return new Hsv(hue, saturation, brightness);
	}
}


public readonly record struct Hsv(Single Hue, Single Saturation, Single Value);
