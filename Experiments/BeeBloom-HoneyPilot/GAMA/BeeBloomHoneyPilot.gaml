model BeeBloomHoneyPilot

// Gameplay experiment, not a calibrated botanical or honey production model.
// Coordinates in messages are GAMA X,Y,Z = Unity X,Z,Y, in metres.
global {
    geometry shape <- rectangle(200, 200);
    float step <- 0.1;
    string pilot_schema <- "beebloom-honey-pilot-v1";
    map state <- [];
    map receipts <- [];
    map response <- [];
    string checkpoint <- "";
    point bee_position <- {0,0,0};
    point hand_position <- {0,0,0};
    int last_contact <- -1;
    list flower_positions <- [];
    list nectar_positions <- [];
    list flower_surfaces <- [];
    list flower_radii <- [];
    list slot_coordinates <- [0.0,0.0,0.0];
    // Workspace is a read-only view of confirmed progress. configure() switches
    // the server experiment to gameplay authority before Unity starts it.
    bool workspace_readonly <- true;
    string workspace_checkpoint <- "../Runtime/checkpoints/1f28f796590a4f208226a1de2dfda7c2.json";
    string view_status <- "Waiting for a checkpoint: select Progress file";
    list<point> map_flowers <- [{40,100},{60,100}];
    point map_slot <- {150,70};
    point map_bee <- {100,100};
    point map_chunk <- {100,100};
    bool has_bee_sample <- false;
    // Live observations are rebuilt from Unity; they never grant nectar/pollen.
    map world_entities <- [];
    map world_response <- ["accepted"::false,"reason"::"not_synced"];
    string world_source <- "";
    int world_sequence <- -1;
    string world_signature <- "";
    float map_left <- 0.0;
    float map_top <- 0.0;
    float map_zoom <- 1.0;
    init {
        create pilot_flower number: 2 { flower_index <- index; }
        create pilot_bee number: 1;
        create pilot_slot number: 1;
        create pilot_legend number: 1;
        do read_workspace;
        do frame_map;
    }
    reflex refresh_workspace when: workspace_readonly and cycle mod 5 = 0 {
        do read_workspace;
    }
    reflex refresh_map { do frame_map; }
    action read_workspace {
        if (workspace_readonly and file_exists(workspace_checkpoint)) {
            // A writer may be replacing this JSON. Keep the last complete frame
            // and retry on the next refresh rather than stopping the display.
            try {
                map saved <- json_file(workspace_checkpoint).contents;
                map candidate <- saved["state"];
                if (candidate["schema"] = pilot_schema and length(list(candidate["flowers"])) = 2) {
                    state <- candidate;
                    list fs <- state["flowers"];
                    map a <- fs[0]; map b <- fs[1]; map h <- state["slot"];
                    flower_positions <- [a["position"],b["position"]];
                    slot_coordinates <- h["position"];
                    // Checkpoints contain transactions, not a continuous pose.
                    // Never label this sample as the live player position.
                    has_bee_sample <- false;
                    int newest <- -1;
                    map history <- saved["receipts"];
                    loop receipt over: history.values {
                        map entry <- map(receipt);
                        if (bool(entry["accepted"]) and int(entry["revision"]) > newest) {
                            map request <- from_json(string(entry["signature"]));
                            if (request contains_key "bee") {
                                bee_position <- point(request["bee"]);
                                newest <- int(entry["revision"]);
                                has_bee_sample <- true;
                            }
                        }
                    }
                    view_status <- "READ ONLY | saved transactions | revision " + string(state["revision"]);
                } else { view_status <- "Progress file has an unsupported schema"; }
            } catch { view_status <- "Reading progress; retrying (last complete frame retained)"; }
        } else if (workspace_readonly) { view_status <- "No progress file: choose Progress file in Parameters"; }
    }
    action frame_map {
        if (length(flower_positions) = 2 and !empty(state)) {
            point a <- point(flower_positions[0]); point b <- point(flower_positions[1]);
            point h <- point(slot_coordinates);
            list<point> locations <- [a,b,h];
            if (has_bee_sample) { locations << bee_position; }
            if (state["chunk_state"] != "none") { locations << point(state["chunk_pos"]); }
            loop entry over: world_entities.values { map e <- map(entry); locations << point(e["position"]); }
            float min_x <- min(locations collect each.x); float max_x <- max(locations collect each.x);
            float min_y <- min(locations collect each.y); float max_y <- max(locations collect each.y);
            float span <- max([max_x-min_x,max_y-min_y,4.0]);
            map_left <- min_x-(span-(max_x-min_x))/2;
            map_top <- max_y+(span-(max_y-min_y))/2;
            map_zoom <- 140.0/span;
            map_flowers <- [project_world(a),project_world(b)];
            map_slot <- project_world(h); map_bee <- project_world(bee_position);
            map_chunk <- project_world(point(state["chunk_pos"]));
            ask synced_entity { location <- {25+(entity_position.x-map_left)*map_zoom,30+(map_top-entity_position.y)*map_zoom,0}; }
        }
    }
    point project_world(point p) {
        // +Z Unity = +Y model = north/up in the observation map.
        return {25+(p.x-map_left)*map_zoom,30+(map_top-p.y)*map_zoom,0};
    }
    action sync_world(string world_json) {
        world_response <- ["accepted"::false,"reason"::"invalid_world","source"::world_source,"sequence"::world_sequence];
        try {
            map incoming <- from_json(world_json);
            string source <- string(incoming["source"]);
            int sequence <- int(incoming["sequence"]);
            list entries <- list(incoming["entities"]);
            bool valid <- !workspace_readonly and source != "" and source = world_source
                and incoming["schema"] = "beebloom-world-v1" and length(entries) <= 256;
            map candidate <- [];
            loop item over: entries {
                map e <- map(item); string id <- string(e["id"]); string kind <- string(e["kind"]);
                list pos <- list(e["position"]);
                valid <- valid and id != "" and !(candidate contains_key id)
                    and kind in ["bee","flower","hive"] and length(pos) = 3;
                if (length(pos) = 3) {
                    // Parse now, before changing any active actor, to reject bad data atomically.
                    point parsed <- {float(pos[0]),float(pos[1]),float(pos[2])};
                    candidate[id] <- e;
                }
            }
            bool replay <- sequence = world_sequence and world_json = world_signature;
            valid <- valid and (sequence > world_sequence or replay);
            if (valid) {
                if (!replay) {
                    world_entities <- candidate; world_sequence <- sequence; world_signature <- world_json;
                    ask synced_entity where (!(world_entities contains_key each.entity_id)) { do die; }
                    loop key over: world_entities.keys {
                        map e <- map(world_entities[key]);
                        list<synced_entity> found <- synced_entity where (each.entity_id = key);
                        if (empty(found)) { create synced_entity with: [entity_id::key]; }
                        ask synced_entity where (each.entity_id = key) {
                            entity_kind <- string(e["kind"]); entity_position <- point(e["position"]);
                            entity_label <- string(e["label"]);
                        }
                    }
                    do frame_map;
                }
                world_response <- ["accepted"::true,"reason"::(replay ? "replayed" : "synced"),
                    "source"::world_source,"sequence"::world_sequence,"count"::length(world_entities),
                    "bees"::length(synced_entity where (each.entity_kind = "bee")),
                    "flowers"::length(synced_entity where (each.entity_kind = "flower")),
                    "hives"::length(synced_entity where (each.entity_kind = "hive")),"entities"::world_entities.values];
            }
        } catch { world_response <- ["accepted"::false,"reason"::"invalid_world","source"::world_source,"sequence"::world_sequence]; }
    }
    action configure(string config_json) {
        if (workspace_readonly) { state <- []; receipts <- []; }
        workspace_readonly <- false;
        has_bee_sample <- true;
        view_status <- "UNITY SERVER | current simulation positions";
        map config <- from_json(config_json);
        checkpoint <- string(config["checkpoint"]);
        if (file_exists(checkpoint)) {
            map saved <- json_file(checkpoint).contents;
            state <- saved["state"];
            receipts <- saved["receipts"];
        }
        if (empty(state)) {
            state <- ["schema"::pilot_schema, "session"::config["session"], "revision"::0,
                "flowers"::config["flowers"], "slot"::config["slot"],
                "contact_radius"::config["contact_radius"], "grab_radius"::config["grab_radius"],
                "snap_radius"::config["snap_radius"], "nectar"::[1,1],
                "pollinated"::[false,false], "pollen_source"::-1, "visits"::0, "last_contact"::-1,
                "chunk_id"::"", "chunk_state"::"none", "chunk_pos"::[0.0,0.0,0.0],
                "stored"::0];
            do persist;
        }
        bool matches <- state["schema"] = pilot_schema and state["session"] = config["session"]
            and state["flowers"] = config["flowers"] and state["slot"] = config["slot"]
            and state["contact_radius"] = config["contact_radius"]
            and state["grab_radius"] = config["grab_radius"] and state["snap_radius"] = config["snap_radius"];
        response <- ["event_id"::"configure", "accepted"::matches,
            "reason"::(matches ? "ready" : "session_or_geometry_mismatch")];
        if (matches) {
            string source <- config contains_key "world_source" ? string(config["world_source"]) : "";
            if (source != world_source) {
                world_source <- source; world_sequence <- -1; world_signature <- "";
                world_entities <- []; ask synced_entity { do die; }
                world_response <- ["accepted"::false,"reason"::"awaiting_world","source"::world_source,"sequence"::world_sequence];
            }
        }
        last_contact <- int(state["last_contact"]);
        list configured_flowers <- state["flowers"];
        map configured_a <- configured_flowers[0];
        map configured_b <- configured_flowers[1];
        flower_positions <- [configured_a["position"], configured_b["position"]];
        nectar_positions <- [];
        flower_surfaces <- []; flower_radii <- [];
        map configured_slot <- state["slot"];
        slot_coordinates <- configured_slot["position"];
    }
    action persist {
        if (!workspace_readonly) {
            state["last_contact"] <- last_contact;
            save json_file(checkpoint, ["state"::state, "receipts"::receipts]) rewrite: true;
        }
    }
    action set_pose(string pose_json) {
        map pose <- from_json(pose_json);
        bee_position <- point(pose["bee"]);
        hand_position <- point(pose["hand"]);
        if (pose contains_key "flowers") { flower_positions <- pose["flowers"]; }
        if (pose contains_key "nectar_positions") { nectar_positions <- pose["nectar_positions"]; }
        if (pose contains_key "flower_surfaces") { flower_surfaces <- pose["flower_surfaces"]; }
        if (pose contains_key "flower_radii") { flower_radii <- pose["flower_radii"]; }
        if (pose contains_key "slot") { slot_coordinates <- pose["slot"]; }
        if (!empty(state) and state["chunk_state"] = "ready" and state contains_key "chunk_flower") {
            int origin_flower <- int(state["chunk_flower"]);
            if (origin_flower >= 0) { state["chunk_pos"] <- length(nectar_positions) > origin_flower ? nectar_positions[origin_flower] : flower_positions[origin_flower]; }
        }
        if (last_contact >= 0 and !empty(state)) {
            list flowers <- state["flowers"];
            map f <- flowers[last_contact];
            point surface <- length(flower_surfaces) > last_contact ? point(flower_surfaces[last_contact]) : point(flower_positions[last_contact]);
            float radius <- length(flower_radii) > last_contact ? min(1.5,max(0.0,float(flower_radii[last_contact]))) : 0.0;
            if ((bee_position distance_to surface) > float(state["contact_radius"]) + radius + 0.15) {
                last_contact <- -1;
            }
        }
    }
    action transact(string request_json) {
        if (!workspace_readonly) {
        map r <- from_json(request_json);
        string eid <- string(r["event_id"]);
        if (receipts contains_key eid) {
            response <- receipts[eid];
            if (response["signature"] != to_json(r)) {
                response <- ["event_id"::eid,"accepted"::false,"reason"::"event_id_conflict"];
            }
        } else {
            bool accepted <- false;
            string reason <- "invalid_request";
            string kind <- string(r["kind"]);
            if (!empty(state) and r["session"] = state["session"]) {
                bee_position <- point(r["bee"]);
                hand_position <- point(r["hand"]);
                point held_chunk_position <- r contains_key "held_chunk" ? point(r["held_chunk"]) : hand_position;
                bool held_chunk_valid <- (held_chunk_position distance_to hand_position) <= 0.25;
                int fi <- int(r["flower"]);
                list flowers <- state["flowers"];
                list nectar <- state["nectar"];
                list pollinated <- state["pollinated"];
                if ((kind = "visit" or kind = "collect") and fi >= 0 and fi < length(flowers)) {
                    map f <- flowers[fi];
                    reason <- "too_far_from_flower";
                    point surface <- length(flower_surfaces) > fi ? point(flower_surfaces[fi]) : point(flower_positions[fi]);
                    float radius <- length(flower_radii) > fi ? min(1.5,max(0.0,float(flower_radii[fi]))) : 0.0;
                    if ((bee_position distance_to surface) <= float(state["contact_radius"]) + radius) {
                        if (kind = "visit") {
                            reason <- "same_visit";
                            if (last_contact != fi) {
                                int source <- int(state["pollen_source"]);
                                if (source >= 0 and source != fi) {
                                    map donor <- flowers[source];
                                    if (donor["species"] = f["species"] and donor["plant_id"] != f["plant_id"]) {
                                        pollinated[fi] <- true;
                                    }
                                }
                                state["pollinated"] <- pollinated;
                                state["pollen_source"] <- fi;
                                state["visits"] <- int(state["visits"]) + 1;
                                last_contact <- fi;
                                accepted <- true;
                                reason <- "visited";
                            }
                        } else {
                            reason <- "carrying_or_empty";
                            if (state["chunk_state"] = "none" and int(nectar[fi]) > 0 and int(state["stored"]) = 0) {
                                nectar[fi] <- int(nectar[fi]) - 1;
                                state["nectar"] <- nectar;
                                state["chunk_id"] <- "nectar-" + eid;
                                state["chunk_state"] <- "ready";
                                state["chunk_pos"] <- length(nectar_positions) > fi ? nectar_positions[fi] : flower_positions[fi];
                                state["chunk_flower"] <- fi;
                                accepted <- true;
                                reason <- "collected";
                            }
                        }
                    }
                }
                if (kind = "pick") {
                    reason <- "not_available_or_too_far";
                    if ((state["chunk_state"] = "ready" or state["chunk_state"] = "loose")
                        and r["chunk_id"] = state["chunk_id"]
                        and (hand_position distance_to point(state["chunk_pos"])) <= float(state["grab_radius"])
                        and (bee_position distance_to point(state["chunk_pos"])) <= 2.0) {
                        state["chunk_state"] <- "held";
                        accepted <- true;
                        reason <- "picked";
                    }
                }
                if (kind = "drop") {
                    reason <- "not_held";
                    if (state["chunk_state"] = "held" and r["chunk_id"] = state["chunk_id"]
                        and held_chunk_valid
                        and (bee_position distance_to hand_position) <= 2.0) {
                        state["chunk_state"] <- "loose";
                        state["chunk_pos"] <- [held_chunk_position.x,held_chunk_position.y,held_chunk_position.z];
                        accepted <- true;
                        reason <- "dropped";
                    }
                }
                if (kind = "deposit") {
                    reason <- "wrong_chunk_slot_or_distance";
                    map slot <- state["slot"];
                    if (state["chunk_state"] = "held" and r["chunk_id"] = state["chunk_id"]
                        and r["slot_id"] = slot["id"] and int(state["stored"]) = 0
                        and held_chunk_valid
                        and (held_chunk_position distance_to point(slot_coordinates)) <= float(state["snap_radius"])
                        and (bee_position distance_to point(slot_coordinates)) <= 2.0) {
                        state["stored"] <- 1;
                        state["chunk_state"] <- "stored";
                        state["chunk_pos"] <- slot_coordinates;
                        accepted <- true;
                        reason <- "deposited";
                    }
                }
            } else { reason <- "wrong_session"; }
            if (accepted) { state["revision"] <- int(state["revision"]) + 1; }
            response <- ["event_id"::eid,"accepted"::accepted,"reason"::reason,"revision"::state["revision"],"signature"::to_json(r)];
            receipts[eid] <- response;
            do persist;
        }
        } else { response <- ["accepted"::false,"reason"::"workspace_is_read_only"]; }
    }
}
species pilot_flower {
    int flower_index;
    aspect default {
        if (!empty(state) and world_sequence < 0) {
            list flowers <- state["flowers"]; map f <- flowers[flower_index];
            list ps <- state["pollinated"];
            draw circle(3) at: map_flowers[flower_index] color: (bool(ps[flower_index]) ? #green : #pink) border: #black;
            draw (flower_index = 0 ? "Flower A" : "Flower B") at: map_flowers[flower_index]+(flower_index = 0 ? {0,8} : {0,-8}) color: #black font: font("SansSerif",14);
        }
    }
}
species pilot_bee {
    aspect default {
        if (has_bee_sample and world_sequence < 0) {
            draw triangle(4) at: map_bee color: #yellow border: #black;
        }
    }
}
species pilot_slot {
    aspect default {
        if (!empty(state)) {
            if (world_sequence < 0) {
                draw square(7) at: map_slot color: (int(state["stored"]) > 0 ? #green : #orange) border: #black;
                draw "Hive " + string(state["stored"]) + "/1" at: map_slot+{5,0} color: #black font: font("SansSerif",14);
            }
            if (state["chunk_state"] != "none" and state["chunk_state"] != "held") {
                draw hexagon(2) at: map_chunk color: #orange border: #black;
            }
        }
    }
}
species synced_entity {
    string entity_id;
    string entity_kind;
    string entity_label;
    point entity_position;
    aspect default {
        if (entity_kind = "bee") { draw triangle(3) color: #yellow border: #black; }
        if (entity_kind = "flower") {
            int fi <- entity_id = "flower-a" ? 0 : (entity_id = "flower-b" ? 1 : -1);
            bool received <- fi >= 0 ? bool(list(state["pollinated"])[fi]) : false;
            draw circle(2.7) color: (received ? #green : #pink) border: #black;
        }
        if (entity_kind = "hive") { draw square(5) color: (int(state["stored"]) > 0 and entity_id = "slot-a" ? #green : #orange) border: #black; }
    }
}
species pilot_legend {
    aspect default {
        draw view_status at: {6,7} color: #black font: font("SansSerif",13);
        draw "Green flower: pollen received | Pink: not yet | Orange square: hive slot" at: {6,188} color: #black font: font("SansSerif",12);
        if (workspace_readonly) {
            draw "Checkpoint view; bee movement between transactions is not streamed" at: {6,195} color: #black font: font("SansSerif",12);
        }
        if (world_sequence >= 0) {
            draw ("Live objects: " + string(length(world_entities)) + " | North (+Z Unity) up") at: {6,195} color: #black font: font("SansSerif",12);
        }
    }
}
experiment HoneyPilot type: gui {
    permanent { }
    float minimum_cycle_duration <- 0.1;
    parameter "Progress file (workspace read-only view)" var: workspace_checkpoint;
    output {
        display HoneyPilotMap type: java2D background: #lightgray {
            species pilot_flower; species pilot_slot; species pilot_bee; species synced_entity; species pilot_legend;
        }
        monitor "View mode" value: view_status;
        monitor "Pollen received [A, B]" value: state["pollinated"];
        monitor "Nectar remaining [A, B]" value: state["nectar"];
        monitor "Honey in hive / 1" value: state["stored"];
        monitor "State" value: state;
        monitor "Last transaction" value: response;
        monitor "World sync" value: world_response;
    }
}
