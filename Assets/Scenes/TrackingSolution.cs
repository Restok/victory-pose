using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mediapipe;
using Mediapipe.Unity;

using Stopwatch = System.Diagnostics.Stopwatch;
using System;

public class TrackingSolution : MonoBehaviour
  {
    [SerializeField] private TextAsset _configAsset;
    [SerializeField] private RawImage _screen;
    [SerializeField] private int _width;
    [SerializeField] private int _height;
    [SerializeField] private int _fps;
    public enum ModelComplexity
        {
          Lite = 0,
          Full = 1,
          Heavy = 2,
        }
    private CalculatorGraph _graph;
    private ResourceManager _resourceManager;

    private WebCamTexture _webCamTexture;
    private Texture2D _inputTexture;
    private Color32[] _inputPixelData;
    private Texture2D _outputTexture;
    private Color32[] _outputPixelData;
    private int outputRotation = 0;
    private bool outputHorizontallyFlipped = false;
    private bool outputVerticallyFlipped = true;
    private ModelComplexity modelComplexity = ModelComplexity.Full;
    private bool smoothLandmarks = true;
    private bool enableSegmentation = false;
    private bool smoothSegmentation = false;
    private int inputRotation = 0;
    private bool inputHorizontallyFlipped = false;
    private bool inputVerticallyFlipped = false;

    private IEnumerator Start()
    {
      if (WebCamTexture.devices.Length == 0)
      {
        throw new System.Exception("Web Camera devices are not found");
      }
      var webCamDevice = WebCamTexture.devices[0];
      _webCamTexture = new WebCamTexture(webCamDevice.name, _width, _height, _fps);
      _webCamTexture.Play();

      yield return new WaitUntil(() => _webCamTexture.width > 16);
      yield return GpuManager.Initialize();

      if (!GpuManager.IsInitialized)
      {
        throw new System.Exception("Failed to initialize GPU resources");
      }

      //_screen.rectTransform.sizeDelta = new Vector2(_width, _height);

      _inputTexture = new Texture2D(_width, _height, TextureFormat.RGBA32, false);
      _inputPixelData = new Color32[_width * _height];
      _outputTexture = new Texture2D(_width, _height, TextureFormat.RGBA32, false);
      _outputPixelData = new Color32[_width * _height];

      _screen.texture = _outputTexture;

      _resourceManager = new StreamingAssetsResourceManager();
      yield return _resourceManager.PrepareAssetAsync("pose_detection.bytes", "pose_detection.bytes", false);
      yield return _resourceManager.PrepareAssetAsync("pose_landmark_full.bytes", "pose_landmark_full.bytes", false);

      var stopwatch = new Stopwatch();

      //_graph = new CalculatorGraph(_configAsset.text);
      var config = CalculatorGraphConfig.Parser.ParseFromTextFormat(_configAsset.text);
      Debug.Log("before validatedGraphConfig");
      using (var validatedGraphConfig = new ValidatedGraphConfig())
      {
          Debug.Log("before Initialize");
          validatedGraphConfig.Initialize(config).AssertOk();
          Debug.Log("before CalculatorGraph");
          _graph = new CalculatorGraph(validatedGraphConfig.Config());
      }

      _graph.SetGpuResources(GpuManager.GpuResources).AssertOk();

      var outputVideoStream = new OutputStream<ImageFramePacket, ImageFrame>(_graph, "output_video");
      outputVideoStream.StartPolling().AssertOk();
      //_graph.StartRun(BuildSidePacket()).AssertOk();
      _graph.StartRun().AssertOk();
      stopwatch.Start();

      while (true)
      {
        _inputTexture.SetPixels32(_webCamTexture.GetPixels32(_inputPixelData));
        var imageFrame = new ImageFrame(ImageFormat.Types.Format.Srgba, _width, _height, _width * 4, _inputTexture.GetRawTextureData<byte>());
        var currentTimestamp = stopwatch.ElapsedTicks / (System.TimeSpan.TicksPerMillisecond / 1000);
        _graph.AddPacketToInputStream("input_video", new ImageFramePacket(imageFrame, new Timestamp(currentTimestamp))).AssertOk();

        yield return new WaitForEndOfFrame();

        if (outputVideoStream.TryGetNext(out var outputVideo))
        {
          if (outputVideo.TryReadPixelData(_outputPixelData))
          {
            _outputTexture.SetPixels32(_outputPixelData);
            _outputTexture.Apply();
          }
        }
      }
    }
    private SidePacket BuildSidePacket()
    {
      var sidePacket = new SidePacket();
      sidePacket.Emplace("output_rotation", new IntPacket((int)outputRotation));
      sidePacket.Emplace("output_horizontally_flipped", new BoolPacket(outputHorizontallyFlipped));
      sidePacket.Emplace("output_vertically_flipped", new BoolPacket(outputVerticallyFlipped));
      sidePacket.Emplace("input_rotation", new IntPacket((int)inputRotation));
      sidePacket.Emplace("input_horizontally_flipped", new BoolPacket(inputHorizontallyFlipped));
      sidePacket.Emplace("input_vertically_flipped", new BoolPacket(inputVerticallyFlipped));
      sidePacket.Emplace("model_complexity", new IntPacket((int)modelComplexity));
      sidePacket.Emplace("smooth_landmarks", new BoolPacket(smoothLandmarks));
      sidePacket.Emplace("enable_segmentation", new BoolPacket(enableSegmentation));
      sidePacket.Emplace("smooth_segmentation", new BoolPacket(smoothSegmentation));

      return sidePacket;
    }
    private void OnDestroy()
    {
      if (_webCamTexture != null)
      {
        _webCamTexture.Stop();
      }

      if (_graph != null)
      {
        try
        {
          _graph.CloseInputStream("input_video").AssertOk();
          _graph.WaitUntilDone().AssertOk();
        }
        finally
        {

          _graph.Dispose();
        }
      }

      GpuManager.Shutdown();
    }
  }