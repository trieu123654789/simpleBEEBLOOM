import assert from 'node:assert/strict';
import { installHeartbeatCleanup } from './SimpleHeartbeatCompat.mjs';

class Manager {
 playerList = new Map();
 notices = [];
 notifyPlayerChange(id) { this.notices.push(id); return 'original notification'; }
 removePlayer(id) {
  const key = this.playerList.has(id) ? id : [...this.playerList].find(([, value]) => value.id === id)?.[0];
  this.playerList.delete(key); return 'original removal';
 }
}
const cancelled = [];
const restore = installHeartbeatCleanup(Manager, timer => cancelled.push(timer));
try {
 const manager = new Manager();
 const player = {};
 manager.playerList.set('quest', player);
 manager.notifyPlayerChange('quest'); // Registration before its first timer exists.
 player.timeout = { socket: 1 };
 assert.equal(manager.notifyPlayerChange('quest'), 'original notification');
 assert.equal(cancelled.length, 0);
 const first = player.timeout;
 player.timeout = { socket: 2 }; // New socket opens before the old close callback.
 const second = player.timeout;
 manager.notifyPlayerChange('quest');
 assert.deepEqual(cancelled, [first]);
 manager.notifyPlayerChange('quest');
 assert.deepEqual(cancelled, [first]); // Ordinary state updates keep the current timer.
 const other = { timeout: { socket: 'other' } };
 manager.playerList.set('other', other);
 manager.notifyPlayerChange('other');
 player.timeout = { socket: 3 };
 manager.notifyPlayerChange('quest');
 assert.deepEqual(cancelled, [first, second]); // Repeated reconnects retire each predecessor.
 assert.ok(!cancelled.includes(player.timeout));
 assert.ok(!cancelled.includes(other.timeout));
 manager.playerList.delete('quest');
 manager.notifyPlayerChange('quest'); // Removing a player remains safe.
 assert.equal(manager.notices.length, 7);
 const replacement = {id:'Player_new',timeout:{socket:4}};
 manager.playerList.set('quest',replacement);manager.notifyPlayerChange('quest');
 assert.equal(manager.removePlayer('Player_new'),'original removal');
 assert.ok(cancelled.includes(replacement.timeout));
 assert.ok(!manager.playerList.has('quest'));
 const rejoined = {id:'Player_rejoined',timeout:{socket:5}};
 manager.playerList.set('quest',rejoined);manager.notifyPlayerChange('quest');
 assert.ok(!cancelled.includes(rejoined.timeout));
 assert.ok(!cancelled.includes(other.timeout));
 assert.equal(manager.removePlayer('quest'),'original removal');
 assert.ok(cancelled.includes(rejoined.timeout));
 console.log('Heartbeat checks passed: reconnect and aggressive removal cancel stale timers; rejoined/other sockets and original methods preserved.');
} finally { restore(); }
