using Godot;
using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Net.Sockets;

using Concentus;
using System.Runtime.InteropServices;


public class UserData
{
    public string username { get; set; }
    public string ip_address { get; set; }
}

public class SessionResponse
{
    public string message { get; set; }
    public int session_port { get; set; }
}


public class SessionConfirmationRequest
{
    public string sender { get; set; }
    public string recipient { get; set; }
    public bool accepted { get; set; }
}


public class SessionConfirmationResponse
{
    public string message { get; set; }
	public string sender { get; set; }
    public int data_port { get; set; }
    public int audio_port { get; set; }
}


public class EndSessionRequest
{
	public string user { get; set; }
    public string sender { get; set; }
	public string recipient { get; set; }
}

public class EndSessionResponse
{
    public string message { get; set; }
}




public class AvatarParameters
{
    public float roll { get; set; }
    public float pitch { get; set; }
    public float yaw { get; set; }
    public float ear_left { get; set; }
    public float ear_right { get; set; }
    public float x_ratio_left { get; set; }
    public float y_ratio_left { get; set; }
    public float x_ratio_right { get; set; }
    public float y_ratio_right { get; set; }
    public float mar { get; set; }
    public float mouth_distance { get; set; }
    public float left_hand_x { get; set; }
    public float left_hand_y { get; set; }
    public float right_hand_x { get; set; }
    public float right_hand_y { get; set; }
}



public partial class Controller : Node
{


	public int avatarPort = 0;
	private UdpClient avatarClient;


	public int audioPort = 0;
	private UdpClient audioClient;
	private bool isReceiving = false;


	public string serverAddress = "127.0.0.1";
	public int serverPort = 5000; 
	public int sessionPort = 0;


	private AudioStreamPlayer audioPlayer;
	private AudioStreamWav audioWav;

	private Node Emit;

	private Control CallWindow;

	private Control CallWindow2;

	private Label Label;

	private AudioStreamPlayer ringtonePlayer;

	private MeshInstance3D Mesh;

	private MeshInstance3D Pants;

	private MeshInstance3D Tshirt;

	private Skeleton3D skeleton;

	private Node3D Cuerpo;


	private static byte[] circularDecodedBuffer = new byte[48000];
	private static int blockSize = 0;
	private static int writeIndex = 0;
	private static int readIndex = 0;


	private float roll = 0, pitch = 0, yaw = 0;
    private float ear_left = 0, ear_right = 0;
    private float x_ratio_left = 0, y_ratio_left = 0, x_ratio_right = 0, y_ratio_right = 0;
    private float mar = 0, mouth_dist = 0;
    private float left_hand_x=0, left_hand_y=0, right_hand_x=0, right_hand_y=0;


	public float abs_body_roll_threshold = 30;
    public float abs_body_yaw_threshold = 30;
    public float abs_body_roll_yaw_max = 60;

    public float ear_max_threshold = 0.6f;
    public float ear_min_threshold = 0.1f;


    public float mar_max_threshold = 0.65f;
    public float mar_min_threshold = 0.0f;

    public bool change_mouth_form = false;
    public float mouth_dist_min = 60.0f;
    public float mouth_dist_max = 75.0f;

	public float rollScale = 0.75f; 
	public float pitchScale = 0.75f;
	public float yawScale = 0.75f;   

	public float left_hand_y_min = 0.38f;
	public float left_hand_y_max = 0.92f;
	public float left_shoulder_rotation_min = -1.2f;
	public float left_shoulder_rotation_max = -0.6f;

	public float left_hand_x_min = 0.06f;
	public float left_hand_x_max = 0.35f;
	public float left_elbow_rotation_min = -1.6f;
	public float left_elbow_rotation_max = -2.5f;

	public float right_hand_y_min = 0.37f;
	public float right_hand_y_max = 0.97f;
	public float right_shoulder_rotation_min = 1.2f;
	public float right_shoulder_rotation_max = 0.6f;

	public float right_hand_x_min = 0.7f;
	public float right_hand_x_max = 0.93f;
	public float right_elbow_rotation_min = 2.5f;
	public float right_elbow_rotation_max = 1.6f;


	int neckIndex = -1;
	Transform3D originalNeckPose;

	private int leftElbowIndex = -1;
	private int rightElbowIndex = -1;
	private Transform3D originalLeftElbowPose;
	private Transform3D originalRightElbowPose;

	private int leftShoulderIndex = -1;
	private int rightShoulderIndex = -1;
	private Transform3D originalLeftShoulderPose;
	private Transform3D originalRightShoulderPose;

	

	public override void _Ready()
	{

		// Obtener referencia al nodo AudioStreamPlayer
        audioPlayer = GetNode<AudioStreamPlayer>("Player");
		if (audioPlayer != null)
		{
			GD.Print("AudioStreamPlayer 'Player' found correctly.");
		}
		else
		{
			GD.Print("AudioStreamPlayer 'Player' not found. Check the node path.");
		}


		Mesh = GetNode<MeshInstance3D>("../DeformationSystem_1211/Skeleton3D/template_fullbody");
		if (Mesh != null && Mesh.Mesh is ArrayMesh arrayMesh)
        {
            GD.Print("'Mesh' found correctly.");
            
        }
        else
        {
            GD.Print("'Mesh' not found. Check the node path.");
        }

		Mesh.Visible = false;


		Pants = GetNode<MeshInstance3D>("../pant");
		if (Pants != null && Pants.Mesh is ArrayMesh arrayMeshPant)
        {
            GD.Print("'Pants' found correctly.");
            
        }
        else
        {
            GD.Print("'Pants' not found. Check the node path.");
        }

		Pants.Visible = false;



		Tshirt = GetNode<MeshInstance3D>("../Camiseta");
		if (Tshirt != null && Tshirt.Mesh is ArrayMesh arrayMeshShirt)
        {
            GD.Print("'Tshirt' found correctly.");
            
        }
        else
        {
            GD.Print("'Tshirt' not found. Check the node path.");
        }

		Tshirt.Visible = false;


		skeleton = GetNode<Skeleton3D>("../DeformationSystem_1211/Skeleton3D");
		if (skeleton != null)
        {
            GD.Print("'skeleton' found correctly.");
            
        }
        else
        {
            GD.Print("'skeleton' not found. Check the node path.");
        }



		Emit = GetNode<Node>("Emit");
		if (Emit != null)
		{
			GD.Print("'Emit' found correctly.");
		}
		else
		{
			GD.Print("'Emit' not found. Check the node path.");
		}


		ringtonePlayer = GetNode<AudioStreamPlayer>("ringtonePlayer");
		if (ringtonePlayer != null)
		{
			GD.Print("'ringtonePlayer' found correctly.");
		}
		else
		{
			GD.Print("'ringtonePlayer' not found. Check the node path.");
		}


		CallWindow = GetNode<Control>("VentanaLlamada");
		if (CallWindow != null)
		{
			GD.Print("'CallWindow' found correctly.");
		}
		else
		{
			GD.Print("'CallWindow' not found. Check the node path.");
		}



		Label = GetNode<Label>("VentanaLlamada/VBoxContainer/Label");
		if (Label != null)
		{
			GD.Print("'Label' found correctly.");
		}
		else
		{
			GD.Print("'Label' not found. Check the node path.");
		}


		CallWindow2 = GetNode<Control>("VentanaLlamada2");
		if (CallWindow2 != null)
		{
			GD.Print("'CallWindow2' found correctly.");
		}
		else
		{
			GD.Print("'CallWindow2' not found. Check the node path.");
		}



		InitRegistration();

		// Start listening for the registration request in a separate thread
		Task.Run(ListenForRegistration);


	}



	private void InitRegistration()
	{
		try
		{
			// Create an object to represent the pair's data
			UserData userData = new UserData
			{
				username = "Receptor",
				ip_address = "127.0.0.1",
			};

			// Serialize the object to JSON format
			string jsonData = JsonSerializer.Serialize(userData);

			// Configure the logging endpoint URL on the Flask server
			string url = "http://127.0.0.1:5000/register";

			// Create an HTTP POST request
			System.Net.Http.HttpClient client = new System.Net.Http.HttpClient();
			StringContent content = new StringContent(jsonData, System.Text.Encoding.UTF8, "application/json");

			// Send the request and receive the response from the server
			HttpResponseMessage response = client.PostAsync(url, content).Result;
			string responseText = response.Content.ReadAsStringAsync().Result;

			// Deserialize the JSON response to get the session port
			SessionResponse sessionResponse = JsonSerializer.Deserialize<SessionResponse>(responseText);
			sessionPort = sessionResponse.session_port;

			// Use the session port as needed
			GD.Print("Session port: " + sessionPort);
		}
		catch (Exception e)
		{
			GD.Print("Error sending data to log server: " + e.Message);
		}
	}


	private async Task ListenForRegistration()
	{	
		UdpClient sessionClient = new UdpClient(sessionPort);

		try
		{
			// Wait to receive data
			UdpReceiveResult result = await sessionClient.ReceiveAsync();
			byte[] receivedData = result.Buffer;

			// Convert the received data from hexadecimal to a readable string
			string sessionData = System.Text.Encoding.UTF8.GetString(receivedData);

			// Deserialize the JSON message as a generic JsonElement object
			JsonElement jsonData = JsonSerializer.Deserialize<JsonElement>(sessionData);


			string messageType = jsonData.GetProperty("message_type").GetString();

			if (messageType == "request")
			{
				try
				{
					// Deserialize the JSON message
					SessionConfirmationResponse jsonRequest = JsonSerializer.Deserialize<SessionConfirmationResponse>(sessionData);

					// Extract audio and video ports from the message
					string sessionMessage = jsonRequest.message;
					string sender = jsonRequest.sender;
					audioPort = jsonRequest.audio_port;
					avatarPort = jsonRequest.data_port;

					GD.Print("Message received : " + sessionMessage);

					GD.Print("Audio port : " + audioPort);
					GD.Print("Avatar data port: " + avatarPort);

					Emit.CallDeferred("EmitSignal", sender);



				}
				catch (Exception e)
				{
					GD.Print("Error parsing session data:" + e.Message);
				}
			}

			else if (messageType == "end")
			{	
				try
				{
					string message = jsonData.GetProperty("message").GetString();
					GD.Print("Message received : " + message);

					CallWindow2.CallDeferred("SetVisible", false);
					Mesh.CallDeferred("SetVisible", false);
					Pants.CallDeferred("SetVisible", false);
					Tshirt.CallDeferred("SetVisible", false);


					isReceiving = false;


					GetTree().Quit();

		
				}
				catch (Exception e)
				{
					GD.Print("Error parsing session data: " + e.Message);
				}
	
			}
		}
		catch (Exception e)
		{
			GD.Print("Error receiving registration request: " + e.Message);
		}
		finally
		{
			sessionClient.Dispose(); 
		}
	}

	


	public void _on_emit_request_call(string sender){

        ringtonePlayer.Play();

		Label.Text = "Llamada Entrante de '" + sender + "'";

		CallWindow.Visible = true;

	}

	public async void AcceptCall(){
		try
		{
			//Stop ringtone if it is playing
			if (IsInstanceValid(ringtonePlayer) && ringtonePlayer.Playing)
			{
				ringtonePlayer.Stop();
			}

			CallWindow.Visible = false;


			SessionConfirmationRequest confirmationRequest = new SessionConfirmationRequest
			{
				sender = "Receptor",
				recipient = "Emisor",
				accepted = true
			};

			// Convert the object to JSON format
			string jsonData = JsonSerializer.Serialize(confirmationRequest);

			// Configure the logging endpoint URL on the Flask server
			string url = "http://127.0.0.1:5000/respond_session";

			// Create an HTTP POST request
			System.Net.Http.HttpClient client = new System.Net.Http.HttpClient();
			StringContent content = new StringContent(jsonData, System.Text.Encoding.UTF8, "application/json");

			// Send the request and receive the response from the server
			HttpResponseMessage response = await client.PostAsync(url, content);
			string responseText = await response.Content.ReadAsStringAsync();

			// Deserialize the JSON response to get the session port
			SessionConfirmationResponse requestResponse = JsonSerializer.Deserialize<SessionConfirmationResponse>(responseText);
			string responseMessage = requestResponse.message;

			// Show the response in the console
			GD.Print("Log server response: " + responseMessage);

			InitReception();
		
		}
		catch (Exception e)
		{
			GD.Print("Error sending data to log server: " + e.Message);
		}	

	}

	public async void DeclineCall(){
		try
		{
			//Stop ringtone if it is playing
			if (IsInstanceValid(ringtonePlayer) && ringtonePlayer.Playing)
			{
				ringtonePlayer.Stop();
			}

			CallWindow.Visible = false;


			SessionConfirmationRequest confirmationRequest = new SessionConfirmationRequest
			{
				sender = "Receptor",
				recipient = "Emisor",
				accepted = false
			};

			// Convert the object to JSON format
			string jsonData = JsonSerializer.Serialize(confirmationRequest);

			// Configure the logging endpoint URL on the Flask server
			string url = "http://127.0.0.1:5000/respond_session";

			// Create an HTTP POST request
			System.Net.Http.HttpClient client = new System.Net.Http.HttpClient();
			StringContent content = new StringContent(jsonData, System.Text.Encoding.UTF8, "application/json");

			// Send the request and receive the response from the server
			HttpResponseMessage response = await client.PostAsync(url, content);
			string responseText = await response.Content.ReadAsStringAsync();

			// Deserialize the JSON response to get the session port
			SessionConfirmationResponse requestResponse = JsonSerializer.Deserialize<SessionConfirmationResponse>(responseText);
			string responseMessage = requestResponse.message;

			// Show the response in the console
			GD.Print("Log server response: " + responseMessage);

			await Task.Run(ListenForRegistration);


		}
		catch (Exception e)
		{
			GD.Print("Error sending data to log server: " + e.Message);
		}	

	}

	public async void EndCall(){

		CallWindow2.Visible = false;

		Mesh.Visible = false;
		Pants.Visible = false;
		Tshirt.Visible = false;


		isReceiving = false;


		EndSessionRequest endSessionRequest = new EndSessionRequest
		{
			user = "Receptor",
			sender = "Emisor",
			recipient = "Receptor"

		};

		// Convert the object to JSON format
		string jsonData = JsonSerializer.Serialize(endSessionRequest);

		// Configure the logging endpoint URL on the Flask server
		string url = "http://127.0.0.1:5000/end_session";

		// Create an HTTP POST request
		System.Net.Http.HttpClient client = new System.Net.Http.HttpClient();
		StringContent content = new StringContent(jsonData, System.Text.Encoding.UTF8, "application/json");

		// Send the request and receive the response from the server
		HttpResponseMessage response = await client.PostAsync(url, content);
		string responseText = await response.Content.ReadAsStringAsync();

		// Deserialize the JSON response to get the session port
		EndSessionResponse requestResponse = JsonSerializer.Deserialize<EndSessionResponse>(responseText);
		string responseMessage = requestResponse.message;

		// Show the response in the console
		GD.Print("Log server response: " + responseMessage);

		GetTree().Quit();

	}



	// Launch TCP to receive message from python
	private void InitReception()
	{
		
		// Start receiving audio in a separate process
		Task receiveTask = Task.Run(ReceiveAudio);

		// Start receiving avatar data
    	Task receiveAvatarDataTask = ReceiveAvatarData();
		
		CallWindow2.Visible = true;

		Task.Run(ListenForRegistration);

		InitSkeleton();


	}


	public static byte[] ConvertShortToBytes(short[] samples)
    {
        byte[] bytes = new byte[samples.Length * 2];
        Buffer.BlockCopy(samples, 0, bytes, 0, bytes.Length);
        return bytes;
    }


	public static void WriteCircularDecodedBuffer(byte[] decodedAudio)
	{
		int blockSize = decodedAudio.Length;

		// Calculate the amount of data available in the buffer
		int availableSpace = circularDecodedBuffer.Length - CircularBufferBytesAvailable();

		// If there is not enough space, avoid overwriting raw data
		if (availableSpace < blockSize)
		{
			GD.Print("There is not enough space in the ring buffer to write new data.");
			return;
		}

		//Copy the decoded samples to the circular buffer in a circular fashion
		for (int i = 0; i < blockSize; i++)
		{
			circularDecodedBuffer[writeIndex] = decodedAudio[i];
			writeIndex = (writeIndex + 1) % circularDecodedBuffer.Length;
		}
	}


	public static int CircularBufferBytesAvailable()
	{
		return (writeIndex - readIndex + circularDecodedBuffer.Length) % circularDecodedBuffer.Length;
	}



	private void PlayFromCircularBuffer()
	{
		int blockSize = 960;

		if (CircularBufferBytesAvailable() < blockSize)
		{
			GD.Print("There are not enough samples in the ring buffer to play.");
			return;
		}

		byte[] bytesToPlay = new byte[blockSize * sizeof(short)];

		for (int i = 0; i < bytesToPlay.Length; i++)
		{
			bytesToPlay[i] = circularDecodedBuffer[readIndex];
			readIndex = (readIndex + 1) % circularDecodedBuffer.Length;
		}

		AudioStreamWav audioWav = new AudioStreamWav
		{
			Format = AudioStreamWav.FormatEnum.Format16Bits,
			Stereo = false,
			MixRate = 16000,
			Data = bytesToPlay
		};

		if (audioPlayer != null)
		{
			audioPlayer.Stream = audioWav;
			audioPlayer.Play();
		}
		else
		{
			GD.Print("AudioPlayer is not initialized correctly.");
		}
	}



	private async void ReceiveAudio()
	{
		audioClient = new UdpClient(audioPort);
		isReceiving = true;

		Concentus.Structs.OpusDecoder opusDecoder = new Concentus.Structs.OpusDecoder(16000,1);

		DateTime startTime = DateTime.Now;
		TimeSpan initialBufferTime = TimeSpan.FromMilliseconds(100);

		while (isReceiving)
		{
			try
			{
				UdpReceiveResult result = await audioClient.ReceiveAsync();
				byte[] receivedAudio = result.Buffer;

				GD.Print("Received audio");

				byte[] rtpHeader = new byte[12];
				Array.Copy(receivedAudio, 0, rtpHeader, 0, 12);

				string rtpHeaderString = BitConverter.ToString(rtpHeader);
				GD.Print("Audio RTP header: " + rtpHeaderString);

				byte[] audioData = new byte[receivedAudio.Length - 12];
				Array.Copy(receivedAudio, 12, audioData, 0, receivedAudio.Length - 12);

				int bufferSize = 960;
				short[] pcmSamples = new short[bufferSize];

				int decodedSamples = opusDecoder.Decode(audioData, pcmSamples, bufferSize);

				byte[] decodedBytes = ConvertShortToBytes(pcmSamples);

				WriteCircularDecodedBuffer(decodedBytes);

				if ((DateTime.Now - startTime) >= initialBufferTime)
				{
					if (CircularBufferBytesAvailable() >= blockSize * sizeof(short))
					{
						CallDeferred("PlayFromCircularBuffer");
					}
				}
			}
			catch (Exception e)
			{
				GD.Print("Error receiving UDP data: " + e.ToString());
			}
		}
	}



	private async Task ReceiveAvatarData()
	{
		avatarClient = new UdpClient(avatarPort);
		isReceiving = true;

		while (isReceiving)
		{
			try
			{
				// Wait to receive data asynchronously
				UdpReceiveResult result = await avatarClient.ReceiveAsync();
				byte[] receivedAudio = result.Buffer;

				GD.Print("Avatar data received");

				// Extract the RTP header (the first 12 bytes)
				byte[] rtpHeader = new byte[12];
				Array.Copy(receivedAudio, 0, rtpHeader, 0, 12);

				// Convert the RTP header to a string and then print
				string rtpHeaderString = BitConverter.ToString(rtpHeader);
				GD.Print("Avatar data RTP header: " + rtpHeaderString);

				// Extract the avatar data (the remaining bytes)
				byte[] avatarDataReceived = new byte[receivedAudio.Length - 12];
				Array.Copy(receivedAudio, 12, avatarDataReceived, 0, receivedAudio.Length - 12);

				AvatarParameters avatarData = JsonSerializer.Deserialize<AvatarParameters>(avatarDataReceived); 
				
				// Assigns the received values ​​to the variables
                roll = avatarData.roll;
                pitch = avatarData.pitch;
                yaw = avatarData.yaw;
                ear_left = avatarData.ear_left;
                ear_right = avatarData.ear_right;
                x_ratio_left = avatarData.x_ratio_left;
                y_ratio_left = avatarData.y_ratio_left;
                x_ratio_right = avatarData.x_ratio_right;
                y_ratio_right = avatarData.y_ratio_right;
                mar = avatarData.mar;
                mouth_dist = avatarData.mouth_distance;
                left_hand_x = avatarData.left_hand_x;
                left_hand_y = avatarData.left_hand_y;
                right_hand_x = avatarData.right_hand_x;
                right_hand_y = avatarData.right_hand_y;

				GD.Print(string.Format("Received values - Roll: {0}, Pitch: {1}, Yaw: {2}, Ear Left: {3}, Ear Right: {4}, X Ratio Left: {5}, Y Ratio Left: {6}, X Ratio Right: {7}, Y Ratio Right: {8}, MAR: {9}, Mouth Dist: {10}, left_hand_x: {11}, left_hand_y: {12}, right_hand_x: {13}, right_hand_y: {14}",
    				roll, pitch, yaw, ear_left, ear_right, x_ratio_left, y_ratio_left, x_ratio_right, y_ratio_right, mar, mouth_dist, left_hand_x, left_hand_y, right_hand_x, right_hand_y));



            	UpdateAvatar();


			}
			catch (Exception e)
			{
				GD.Print("Error receiving avatar data: " + e.Message);
			}
		}
	}

	private void UpdateAvatar()
    {
        if (Mesh != null)
        {

			if (Mesh != null)
			{
				Mesh.Visible = true;
				Pants.Visible = true;
				Tshirt.Visible = true;


				EyeBlinking();

				MouthClose();

				NeckMovement();

				ArmsMovement();


    		}			
		}

	}

	void EyeBlinking() {
        // Calculate the value for the left eye (left is right in Godot)
		ear_left = Mathf.Clamp(ear_left, ear_min_threshold, ear_max_threshold);
		float eye_L_value = 0.8f - ((ear_left - ear_min_threshold) / (ear_max_threshold - ear_min_threshold) * 2);
		Mesh.Set("blend_shapes/eyeBlinkLeft", eye_L_value);

		// Check the set value
		var currentEyeLValue = Mesh.Get("blend_shapes/eyeBlinkLeft");
		GD.Print("Current value of eyeBlinkLeft: ", currentEyeLValue);

		// Calculate the value for the right eye (right is left in Godot)
		ear_right = Mathf.Clamp(ear_right, ear_min_threshold, ear_max_threshold);
		float eye_R_value = 0.8f - ((ear_right - ear_min_threshold) / (ear_max_threshold - ear_min_threshold) * 1.5f);
		Mesh.Set("blend_shapes/eyeBlinkRight", eye_R_value);

		// Check the set value
		var currentEyeRValue = Mesh.Get("blend_shapes/eyeBlinkRight");
		GD.Print("Current value of eyeBlinkRight: ", currentEyeRValue);
    }


	void MouthClose()
	{
		// Calculate the value for the mouth
		float mar_value = mar;
		mar_value = Mathf.Clamp(mar_value, mar_min_threshold, mar_max_threshold);
		float mouth_value = 0.3f - ((mar_value - mar_min_threshold) / (mar_max_threshold - mar_min_threshold) * 0.9f);
		Mesh.Set("blend_shapes/mouthClose", mouth_value);

		// Check the set value
		var currentMouthValue = Mesh.Get("blend_shapes/mouthClose");
		GD.Print("Current value of mouthClose: ", currentMouthValue);
	}


	void InitSkeleton(){
		// Find the ID of the neck bone
		neckIndex = skeleton.FindBone("with_rigged_body_Neck_M");
		GD.Print("Neck Bone ID: ", neckIndex);

		// Get the current global pose of the neck bone
		originalNeckPose = skeleton.GetBoneGlobalPose(neckIndex);


		leftElbowIndex = skeleton.FindBone("with_rigged_body_Elbow_L");
		rightElbowIndex = skeleton.FindBone("with_rigged_body_Elbow_R");
		GD.Print("Left Elbow Bone ID: ", leftElbowIndex);
		GD.Print("Right Elbow Bone ID: ", rightElbowIndex);

		originalLeftElbowPose = skeleton.GetBoneGlobalPose(leftElbowIndex);
		originalRightElbowPose = skeleton.GetBoneGlobalPose(rightElbowIndex);


		leftShoulderIndex = skeleton.FindBone("with_rigged_body_Shoulder_L");
		rightShoulderIndex = skeleton.FindBone("with_rigged_body_Shoulder_R");
		GD.Print("Left Shoulder Bone ID: ", leftShoulderIndex);
		GD.Print("Right Shoulder Bone ID: ", rightShoulderIndex);

		originalLeftShoulderPose = skeleton.GetBoneGlobalPose(leftShoulderIndex);
		originalRightShoulderPose = skeleton.GetBoneGlobalPose(rightShoulderIndex);

	}



	void NeckMovement()
	{
		if (neckIndex == -1)
		{
			GD.PrintErr("Error: Neck Bone ID is invalid");
			return;
		}

		// Use the base bone transformation
		Transform3D boneTransform = originalNeckPose;

		// Apply limitations to rotation angles
		float clampedRoll = Mathf.Clamp(roll, -30f, 30f) * rollScale;
		float clampedPitch = Mathf.Clamp(pitch, -30f, 30f) * pitchScale;
		float clampedYaw = Mathf.Clamp(yaw, -30f, 30f) * yawScale;

		float radianConverter = (float)(Math.PI / 180.0);

        // Convert the angles of roll, pitch, yaw to radians
        float rollRadians = -clampedRoll * radianConverter;
        float pitchRadians = -clampedPitch * radianConverter;
        float yawRadians = -clampedYaw * radianConverter;

		GD.Print($"Clamped Roll: {clampedRoll}, Clamped Pitch: {clampedPitch}, Clamped Yaw: {clampedYaw}");

		// Apply rotations on the base transformation
		boneTransform = boneTransform.RotatedLocal(Vector3.Right, pitchRadians); 
		boneTransform = boneTransform.RotatedLocal(Vector3.Forward, rollRadians); 
		boneTransform = boneTransform.RotatedLocal(Vector3.Up, yawRadians);       

		// Apply the new bone pose
		skeleton.SetBoneGlobalPoseOverride(neckIndex, boneTransform, 1.0f, true);

	}



	void ArmsMovement()
	{

		if (left_hand_x == 0 && left_hand_y == 0)
		{
			skeleton.SetBoneGlobalPoseOverride(leftElbowIndex, originalLeftElbowPose, 1.0f, true);
			skeleton.SetBoneGlobalPoseOverride(leftShoulderIndex, originalLeftShoulderPose, 1.0f, true);
		}
		else
		{

			// Get the relative position of left_hand_y within its range
			float leftHandYPositionRatio = (left_hand_y - left_hand_y_min) / (left_hand_y_max - left_hand_y_min);

			// Map handYPositionRatio to shoulder rotation range
			float targetLeftShoulderRotation = Mathf.Lerp(left_shoulder_rotation_min, left_shoulder_rotation_max, leftHandYPositionRatio);

			// Move left shoulder to new position
			Transform3D shoulderLeftTransform = originalLeftShoulderPose;
			shoulderLeftTransform = shoulderLeftTransform.RotatedLocal(Vector3.Forward, targetLeftShoulderRotation);
			skeleton.SetBoneGlobalPoseOverride(leftShoulderIndex, shoulderLeftTransform, 1.0f, true);


			// Get the updated global transformation of the left shoulder
			Transform3D currentLeftShoulderTransform = skeleton.GetBoneGlobalPose(leftShoulderIndex);

			//Get the original global transformation of the left elbow (without accumulated rotations)
			Transform3D originalLeftElbowRelativeTransform = originalLeftShoulderPose.AffineInverse() * originalLeftElbowPose;


			// Get the relative position of left_hand_x within its range
			float leftHandPositionRatio = (left_hand_x - left_hand_x_min) / (left_hand_x_max - left_hand_x_min);

			// Map leftHandPositionRatio to elbow rotation range (inverted)
			float targetLeftElbowRotation = Mathf.Lerp(left_elbow_rotation_min, left_elbow_rotation_max, leftHandPositionRatio);

			// Apply rotation to the left elbow in its local space
			Transform3D newLeftElbowRelativeTransform = originalLeftElbowRelativeTransform.RotatedLocal(Vector3.Forward, targetLeftElbowRotation);
			newLeftElbowRelativeTransform = newLeftElbowRelativeTransform.RotatedLocal(Vector3.Up, -0.5f);

			// Transform elbow back to skeleton global space
			Transform3D newLeftElbowTransform = currentLeftShoulderTransform * newLeftElbowRelativeTransform;

			// Set the new left elbow pose
			skeleton.SetBoneGlobalPoseOverride(leftElbowIndex, newLeftElbowTransform, 1.0f, true);

		}


		if (right_hand_x == 0 && right_hand_y == 0)
		{
			skeleton.SetBoneGlobalPoseOverride(rightElbowIndex, originalRightElbowPose, 1.0f, true);
			skeleton.SetBoneGlobalPoseOverride(rightShoulderIndex, originalRightShoulderPose, 1.0f, true);
		}
		else
		{

			// Get the relative position of right_hand_y within its range
			float rightHandYPositionRatio = (right_hand_y - right_hand_y_min) / (right_hand_y_max - right_hand_y_min);

			// Map handYPositionRatio to shoulder rotation range
			float targetRightShoulderRotation = Mathf.Lerp(right_shoulder_rotation_min, right_shoulder_rotation_max, rightHandYPositionRatio);


			// Move the right shoulder to the new position
			Transform3D shoulderRightTransform = originalRightShoulderPose;
			shoulderRightTransform = shoulderRightTransform.RotatedLocal(Vector3.Forward, targetRightShoulderRotation);
			skeleton.SetBoneGlobalPoseOverride(rightShoulderIndex, shoulderRightTransform, 1.0f, true);

			// Get the updated global transformation of the right shoulder
			Transform3D currentRightShoulderTransform = skeleton.GetBoneGlobalPose(rightShoulderIndex);

			// Get the original global transformation of the right elbow (without accumulated rotations)
			Transform3D originalRightElbowRelativeTransform = originalRightShoulderPose.AffineInverse() * originalRightElbowPose;


			// Get the relative position of right_hand_x within its range
			float rightHandPositionRatio = (right_hand_x - right_hand_x_min) / (right_hand_x_max - right_hand_x_min);

			// Map rightHandPositionRatio to right elbow rotation range
			float targetRightElbowRotation = Mathf.Lerp(right_elbow_rotation_min, right_elbow_rotation_max, rightHandPositionRatio);
			

			// Apply rotation to the right elbow in its local space
			Transform3D newRightElbowRelativeTransform = originalRightElbowRelativeTransform.RotatedLocal(Vector3.Forward, targetRightElbowRotation);
			newRightElbowRelativeTransform = newRightElbowRelativeTransform.RotatedLocal(Vector3.Up, 0.5f);

			// Transform elbow back to skeleton global space
			Transform3D newRightElbowTransform = currentRightShoulderTransform * newRightElbowRelativeTransform;

			// Set the new right elbow pose
			skeleton.SetBoneGlobalPoseOverride(rightElbowIndex, newRightElbowTransform, 1.0f, true);

		}

	}

	
}








