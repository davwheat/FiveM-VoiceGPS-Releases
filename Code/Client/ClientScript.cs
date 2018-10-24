using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using CitizenFX.Core.UI;

namespace VoiceGPS_FiveM.Client
{
	public class ClientScript : BaseScript
	{
		public ClientScript()
		{
			Chat("VGPS Loaded");

			EventHandlers.Add("vgps:toggleVGPS", new Action(this.ToggleVgps));
			EventHandlers.Add("vgps:adjustVolume", new Action<List<object>>(this.ChangeVolume));

			Tick += RunTick;

			Chat("^2This server uses VoiceGPS!");
			Chat("^2Created by ^1github.com/davwheat");
			Chat("^2Toggle VoiceGPS on and off using ^1/vgps");

			_playerPed = GetPlayerPed();
		}

		private void ChangeVolume(List<object> args)
		{
			double num;

			if (!double.TryParse(args[0].ToString(), out num))
			{
				ShowNotification("~r~VoiceGPS volume must be a valid number between 1.0 and 0.0!", false);
			}
			else
			{
				if (num > 1.0 || num < 0.0)
				{
					ShowNotification("~r~VoiceGPS volume must be a valid number between 1.0 and 0.0!", false);
				}
				else
				{
					_audioVolume = num;
				}
			}
		}

		private async Task RunTick()
		{
			if (_playerPed.IsInVehicle() && _voiceGpsEnabled)
			{
				Blip blip = World.GetWaypointBlip();

				if (Game.IsWaypointActive || blip == null)
				{
					if (_playedStartDriveAudio && !_justPlayedArrived)
					{
						PlayAudio("arrived");
						_justPlayedArrived = true;
						await BaseScript.Delay(2000);
					}
					_playedStartDriveAudio = false;
					await BaseScript.Delay(1000);
				}
				else
				{
					_justPlayedArrived = false;
					if (!_playedStartDriveAudio)
					{
						PlayAudio("start");
						await Delay(2600);

						_playedStartDriveAudio = true;
					}
					else
					{
						_justPlayedArrived = false;
						Tuple<int, float, float> directionInfo = GenerateDirectionsToCoord(blip.Position);

						int dist = (int)Math.Round(directionInfo.Item3);
						int dir = directionInfo.Item1;

						if (dir > 8 || dir < 0)
						{
							await BaseScript.Delay(1000);
						}
						else
						{
							if (_lastDirection != dir)
							{
								_justPlayed200M = false;
								_justPlayedImmediate = false;
								_justPlayed1000M = false;
							}
							if (dist > 175 && dist < 300 && !_justPlayed200M && dir != 5)
							{
								PlayAudio("200m");
								_justPlayed200M = true;
								await BaseScript.Delay(2300);
								PlayDirectionAudio(dir);
							}
							if (dist > 500 && dist < 1000 && !_justPlayed1000M && dir != 5)
							{
								PlayAudio("1000m");
								_justPlayed1000M = true;

								await Delay(2300);
								PlayDirectionAudio(dir);
							}
							if (!_justPlayedImmediate && dist < 55 && dist > 20 && dir != 5)
							{
								_justPlayedImmediate = true;
								PlayDirectionAudio(dir);
							}
							else if (dist < 20 && dir != 5)
							{
								_lastDirection = 0;
								_justPlayed1000M = false;
								_justPlayed200M = false;
								_justPlayedImmediate = true;
							}
							if (dir == 2 && !_justPlayedFollowRoad && _lastDirection != 5)
							{
								_justPlayedFollowRoad = true;
							}
							else if (dir != 2)
							{
								_justPlayedFollowRoad = false;
							}
							if (dir == 1 && !this._justPlayedRecalc)
							{
								_justPlayedRecalc = true;
								PlayAudio("recalculating");

								await Delay(3000);
							}
							else if (dir != 1)
							{
								_justPlayedRecalc = false;
							}

							_lastDirection = dir;
							_justPlayedArrived = true;

							blip = null;
							directionInfo = null;
						}
					}
				}
			}
		}

		private string DirectionToString(int direction)
		{
			switch (direction)
			{
                case 0:
                    return "You have arrived (0)";
                case 1:
                    return "Recalculating (1)";
                case 2:
                    return "Follow the road (2)";
                case 3:
                    return "Left at next junction (3)";
                case 4:
                    return "Right at next junction (4)";
                case 5:
                    return "Straight at next junction (5)";
                case 6:
                    return "Keep left (6)";
                case 7:
                    return "Keep right (7)";
                case 8:
                    return "Unknown (8)";
                default:
                    return $"Unknown ({direction})";
            }
		}

		private void PlayDirectionAudio(int dir)
		{
			switch (dir)
			{
                case 1:
                    PlayAudio("recalculating");
                    break;
                default:
                    Chat(dir.ToString());
                    break;
                case 3:
                    PlayAudio("turnleft");
                    break;
                case 4:
                    PlayAudio("turnright");
                    break;
                case 5:
                    break;
                case 6:
                    PlayAudio("keepleft");
                    break;
                case 7:
                    PlayAudio("keepright");
                    break;
            }
		}

		private void ToggleVgps()
		{
			_voiceGpsEnabled = !_voiceGpsEnabled;

			if (!_voiceGpsEnabled)
			{
				_lastDirection = 0;
				_justPlayed200M = _justPlayedArrived = _justPlayedFollowRoad = _justPlayedImmediate = _justPlayedRecalc = _justPlayed1000M = false;
			}

			ShowNotification(_voiceGpsEnabled ? "Voice GPS ~g~ENABLED" : "Voice GPS ~r~DISABLED", false);
		}

		public Tuple<int, float, float> GenerateDirectionsToCoord(Vector3 position)
		{
			OutputArgument direction = new OutputArgument();
			OutputArgument vehicle = new OutputArgument();
			OutputArgument distToNxJunction = new OutputArgument();

			Function.Call<int>(Hash.GENERATE_DIRECTIONS_TO_COORD, new InputArgument[]
			{
				position.X,
				position.Y,
				position.Z,
				true,
				direction,
				vehicle,
				distToNxJunction
			});

			return new Tuple<int, float, float>(direction.GetResult<int>(), vehicle.GetResult<float>(), distToNxJunction.GetResult<float>());
		}

		public Ped GetPlayerPed()
		{
			return Game.PlayerPed;
		}

		private void Chat(string msg)
		{
			TriggerEvent("chatMessage", new object[]
			{
				"VoiceGPS",
				new int[]
				{
					255,
					255,
					255
				},
				msg
			});
		}

		private void ShowNotification(string msg, bool blinking = false)
		{
			Screen.ShowNotification(msg, blinking);
		}

		private void PlayAudio(string filename)
		{
            string msg = $"{{\"type\":\"playGPSSound\",\"audioFile\":\"{filename}\",\"volume\":{_audioVolume}}}";

			Debug.WriteLine(msg);
			API.SendNuiMessage(msg);
		}

		private static Ped _playerPed;
		private double _audioVolume = 0.7;
		private bool _justPlayed1000M;
		private bool _justPlayed200M;
		private bool _justPlayedFollowRoad;
		private bool _justPlayedImmediate;
		private bool _playedStartDriveAudio;
		private bool _justPlayedRecalc;
		private bool _voiceGpsEnabled;
		private bool _justPlayedArrived = true;
		private int _lastDirection;
	}
}
