// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Logging;
using osu.Game.Beatmaps;
using osu.Game.Online.API;
using osu.Game.Online.Rooms;
using osu.Game.Rulesets;

namespace osu.Game.Screens.OnlinePlay.Components
{
    public class RoomManager : Component, IRoomManager
    {
        public event Action RoomsUpdated;

        private readonly BindableList<Room> rooms = new BindableList<Room>();

        public IBindableList<Room> Rooms => rooms;

        protected IBindable<Room> JoinedRoom => joinedRoom;
        private readonly Bindable<Room> joinedRoom = new Bindable<Room>();

        [Resolved]
        private RulesetStore rulesets { get; set; }

        [Resolved]
        private BeatmapManager beatmaps { get; set; }

        [Resolved]
        private IAPIProvider api { get; set; }

        public RoomManager()
        {
            RelativeSizeAxes = Axes.Both;
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);
            PartRoom();
        }

        public virtual void CreateRoom(Room room, Action<Room> onSuccess = null, Action<string> onError = null)
        {
            room.Host.Value = api.LocalUser.Value;

            var req = new CreateRoomRequest(room);

            req.Success += result =>
            {
                joinedRoom.Value = room;

                update(room, result);
                addRoom(room);

                RoomsUpdated?.Invoke();
                onSuccess?.Invoke(room);
            };

            req.Failure += exception =>
            {
                onError?.Invoke(req.Result?.Error ?? exception.Message);
            };

            api.Queue(req);
        }

        private JoinRoomRequest currentJoinRoomRequest;

        public virtual void JoinRoom(Room room, string password = null, Action<Room> onSuccess = null, Action<string> onError = null)
        {
            currentJoinRoomRequest?.Cancel();
            currentJoinRoomRequest = new JoinRoomRequest(room, password);

            currentJoinRoomRequest.Success += () =>
            {
                joinedRoom.Value = room;
                onSuccess?.Invoke(room);
            };

            currentJoinRoomRequest.Failure += exception =>
            {
                if (!(exception is OperationCanceledException))
                    Logger.Log($"Failed to join room: {exception}", level: LogLevel.Important);
                onError?.Invoke(exception.ToString());
            };

            api.Queue(currentJoinRoomRequest);
        }

        public virtual void PartRoom()
        {
            currentJoinRoomRequest?.Cancel();

            if (JoinedRoom.Value == null)
                return;

            api.Queue(new PartRoomRequest(joinedRoom.Value));
            joinedRoom.Value = null;
        }

        private readonly HashSet<long> ignoredRooms = new HashSet<long>();

        public void AddOrUpdateRoom(Room room)
        {
            Debug.Assert(room.RoomID.Value != null);

            if (ignoredRooms.Contains(room.RoomID.Value.Value))
                return;

            room.Position.Value = -room.RoomID.Value.Value;

            try
            {
                update(room, room);
                addRoom(room);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Failed to update room: {room.Name.Value}.");

                ignoredRooms.Add(room.RoomID.Value.Value);
                rooms.Remove(room);
            }

            notifyRoomsUpdated();
        }

        public void RemoveRoom(Room room)
        {
            rooms.Remove(room);
            notifyRoomsUpdated();
        }

        public void ClearRooms()
        {
            rooms.Clear();
            notifyRoomsUpdated();
        }

        /// <summary>
        /// Updates a local <see cref="Room"/> with a remote copy.
        /// </summary>
        /// <param name="local">The local <see cref="Room"/> to update.</param>
        /// <param name="remote">The remote <see cref="Room"/> to update with.</param>
        private void update(Room local, Room remote)
        {
            foreach (var pi in remote.Playlist)
                pi.MapObjects(beatmaps, rulesets);

            local.CopyFrom(remote);
        }

        /// <summary>
        /// Adds a <see cref="Room"/> to the list of available rooms.
        /// </summary>
        /// <param name="room">The <see cref="Room"/> to add.</param>
        private void addRoom(Room room)
        {
            var existing = rooms.FirstOrDefault(e => e.RoomID.Value == room.RoomID.Value);
            if (existing == null)
                rooms.Add(room);
            else
                existing.CopyFrom(room);
        }

        private void notifyRoomsUpdated() => Scheduler.AddOnce(() => RoomsUpdated?.Invoke());
    }
}
