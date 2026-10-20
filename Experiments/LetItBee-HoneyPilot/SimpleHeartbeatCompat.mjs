// Keep one heartbeat interval per player when a new socket replaces an old one.
// WebPlatform 4.0 overwrites player.timeout on reconnect; the old socket's close
// handler correctly ignores the replacement socket, but leaves its old timer alive.
// This adapter keeps the original wire protocol and current heartbeat unchanged.
export function installHeartbeatCleanup(PlayerManager, cancel = clearInterval) {
 const previous = new WeakMap();
 const notify = PlayerManager.prototype.notifyPlayerChange;
 const remove = PlayerManager.prototype.removePlayer;
 PlayerManager.prototype.notifyPlayerChange = function (id) {
  const player = this.playerList.get(id);
  if (player) {
   const old = previous.get(player);
   const current = player.timeout;
   if (old && old !== current) cancel(old);
   if (current) previous.set(player, current);
   else previous.delete(player);
  }
  return notify.call(this, id);
 };
 if (typeof remove === 'function') {
  PlayerManager.prototype.removePlayer = function (id) {
   let player = this.playerList.get(id);
   if (!player) {
    for (const candidate of this.playerList.values()) {
     if (candidate.id === id) { player = candidate; break; }
    }
   }
   if (player) {
    // Aggressive removal deletes the map entry before the socket close event.
    // Its close handler cannot find the player to clear this timer afterwards;
    // the orphan then pings/kills a new player registered at the same IP.
    const old = previous.get(player);
    if (old && old !== player.timeout) cancel(old);
    if (player.timeout) cancel(player.timeout);
    previous.delete(player);
   }
   return remove.call(this, id);
  };
 }
 return () => {
  PlayerManager.prototype.notifyPlayerChange = notify;
  if (typeof remove === 'function') PlayerManager.prototype.removePlayer = remove;
 };
}
