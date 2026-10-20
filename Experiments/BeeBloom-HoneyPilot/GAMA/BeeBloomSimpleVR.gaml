model BeeBloomSimpleVR
import "BeeBloomHoneyPilot.gaml"

global {
    string pilot_owner <- "";
    // Relative to the model; moving the project does not require an APK rebuild.
    string simple_checkpoint_directory <- "../Runtime/checkpoints";
    init {
        if (file_exists("server.local.json")) {
            map settings <- json_file("server.local.json").contents;
            simple_checkpoint_directory <- string(settings["checkpoint_directory"]);
        }
    }
}

// LinkToUnity owns player registration and routes messages through SIMPLE.
// The existing Unity scene owns visuals and locomotion; no geometry/spawn data
// from this experiment is applied to the saved XR rig.
species unity_linker parent: abstract_unity_linker {
    string player_species <- string(unity_player);
    int min_num_players <- 1;
    int max_num_players <- 1;
    bool do_send_world <- false;

    // Compatibility with the ConnectionManager already present in this project.
    // SIMPLE creates the player first; this notification must be idempotent.
    action new_connection(string id) { }

    action bee_request(string id, string envelope_json) {
        list<unity_player> receivers <- unity_player where (each.name = id);
        if (!empty(receivers)) {
            map envelope <- from_json(envelope_json);
            string op <- string(envelope["operation"]);
            map payload <- envelope["payload"];
            // A disconnected headset may have been removed without a clean
            // goodbye. Keep the checkpoint, but do not leave its ID owning the
            // pilot forever when no such player remains in the simulation.
            if (pilot_owner != "" and empty(unity_player where (each.name = pilot_owner))) {
                pilot_owner <- "";
            }
            bool permitted <- pilot_owner = "" or pilot_owner = id;
            string fault <- permitted ? "" : "pilot_owned_by_another_player";
            if (permitted) {
                if (op = "configure") {
                    map incoming <- from_json(string(payload["config_json"]));
                    string session_key <- string(incoming["session"]);
                    if (empty(regex_matches(session_key,"^[a-f0-9]{32}$"))) {
                        fault <- "invalid_session_key";
                    } else if (!workspace_readonly and incoming["session"] != state["session"]) {
                        fault <- "stop_experiment_before_changing_session";
                    } else {
                        // The server owns paths, including for older APKs that send
                        // an absolute checkpoint path. Keep that path out of authority.
                        file checkpoint_folder <- new_folder(simple_checkpoint_directory);
                        incoming["checkpoint"] <- to_absolute_path(simple_checkpoint_directory + "/" + session_key + ".json");
                        ask world { do configure(to_json(incoming)); }
                        if (bool(response["accepted"])) { pilot_owner <- id; }
                        else { fault <- string(response["reason"]); }
                    }
                } else if (workspace_readonly or pilot_owner != id) {
                    fault <- "configure_required";
                } else {
                    if (op = "set_pose") {
                        ask world { do set_pose(string(payload["pose_json"])); }
                    } else if (op = "transact") {
                        ask world { do transact(string(payload["request_json"])); }
                    } else if (op = "sync_world") {
                        ask world { do sync_world(string(payload["world_json"])); }
                    } else if (op != "snapshot") { fault <- "unknown_operation"; }
                }
            }
            do send_message players: receivers mes: ["bee_bloom"::[
                "correlation"::envelope["correlation"],"fault"::fault,
                "snapshot"::["state"::state,"response"::response,"cycle"::cycle,
                    "flower_positions"::flower_positions,"slot_position"::slot_coordinates,
                    "bee_position"::[bee_position.x,bee_position.y,bee_position.z],"world_sync"::world_response]]];
        }
    }
}
species unity_player parent: abstract_unity_player;

experiment BeeBloomVR parent: HoneyPilot autorun: false type: unity {
    float minimum_cycle_duration <- 0.1;
    string unity_linker_species <- string(unity_linker);
    list<string> displays_to_hide <- [];
    action create_player(string id) {
        if (empty(unity_player where (each.name = id))) { ask unity_linker { do create_player(id); } }
    }
    action remove_player(string id_input) {
        ask unity_player where (each.name = id_input) { do die; }
        if (pilot_owner = id_input) { pilot_owner <- ""; }
    }
}
