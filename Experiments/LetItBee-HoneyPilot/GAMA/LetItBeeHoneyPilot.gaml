model LetItBeeHoneyPilot
import "LetItBeeWater.gaml"

// Gameplay experiment, not a calibrated botanical or honey production model.
// Coordinates in messages are GAMA X,Y,Z = Unity X,Z,Y, in metres.
global {
    geometry shape <- rectangle(200, 200);
    float step <- 0.1;
    string pilot_schema <- "letitbee-honey-pilot-v1";
    string legacy_pilot_schema <- "beebloom-honey-pilot-v1";
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
    list live_hive_slots <- [];
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
    float observation_export_time <- 0.0;
    int observation_unchanged <- 0;
    float observation_received_ms <- -1.0;
    string perched_flower_id <- "";
    float map_left <- 0.0;
    float map_top <- 0.0;
    float map_zoom <- 1.0;
    // Survey geometry is only a footprint, never a substitute for measured salinity.
    string water_footprint_file <- "../Water/gameplay-domain-survey.json";
    list<point> water_footprint_bounds <- [];
    string water_observed_progress <- "";
    int inspected_water_cell <- -1;
    string observation_map_file <- "../Water/gameplay-observation-map.json";
    bool show_environment <- true;
    bool show_salt_grid <- true;
    bool background_ready <- false;
    string background_status <- "No exported environment background";
    list<point> background_bounds <- [];
    point map_hive_center <- {0,0,0};
    action load_observation_background {
        try {
            if (file_exists(observation_map_file)) {
                map bg <- json_file(observation_map_file).contents;
                if (bg["schema"] = "letitbee-observation-map-v1") {
                    list bb <- bg["bounds"];
                    background_bounds <- [{float(bb[0]),float(bb[1]),0},{float(bb[2]),float(bb[3]),0}];
                    loop raw over: list(bg["tiles"]) {
                        map t <- map(raw);
                        create observation_tile with: [world_position::point(t["center"]),world_size::point(t["size"]),is_water::(t["kind"]="water")];
                    }
                    loop raw over: list(bg["landmarks"]) {
                        map t <- map(raw);
                        create observation_landmark with: [world_position::point(t["position"]),world_size::point(t["size"]),landmark_kind::string(t["kind"]),landmark_label::string(t["label"])];
                    }
                    background_ready <- true;
                    background_status <- "Unity export: "+string(bg["captured_at"])+(bool(bg["unsaved_scene"]) ? " | unsaved Editor snapshot" : " | saved scene");
                }
            }
        } catch { background_status <- "Environment export unavailable; grid fallback"; }
    }
    init {
        do load_observation_background;
        try {
            if (file_exists(water_footprint_file)) {
                map survey <- json_file(water_footprint_file).contents;
                if (survey["schema"] = "letitbee-water-grid-v1") {
                    int w <- int(survey["width"]); int h <- int(survey["height"]);
                    float dx <- float(survey["cell_size_m"]); list origin <- list(survey["origin_xz"]);
                    list depths <- list(survey["depth_m"]);
                    if (w > 0 and h > 0 and w*h <= 4096 and dx > 0 and length(depths) = w*h and length(origin) = 2) {
                        water_footprint_bounds <- [{float(origin[0]),float(origin[1]),0},{float(origin[0])+w*dx,float(origin[1])+h*dx,0}];
                        loop j from: 0 to: w*h-1 {
                            if (float(depths[j]) > 0) {
                                create water_footprint with: [world_position::{float(origin[0])+(j mod w+0.5)*dx,float(origin[1])+(int(j/w)+0.5)*dx,0},cell_size::dx];
                            }
                        }
                    }
                }
            }
        } catch { water_view_status <- "Water survey unavailable; waiting for session data"; }
        loop i from: 0 to: 1 { create pilot_flower with: [flower_index::i]; }
        create pilot_bee number: 1;
        create pilot_slot number: 1;
        create pilot_legend number: 1;
        do read_workspace;
        do frame_map;
    }
    reflex refresh_workspace when: workspace_readonly and cycle mod 2 = 0 {
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
                if ((candidate["schema"] = pilot_schema or candidate["schema"] = legacy_pilot_schema) and length(list(candidate["flowers"])) >= 2) {
                    state <- candidate;
                    if (water_observed_progress != workspace_checkpoint) {
                        // A different progress file must never display the previous session's salt.
                        ask water_cell { do die; }
                        water_ready <- false; water_signature <- ""; water_salt <- []; water_volume <- [];
                        water_time_seconds <- 0.0; water_revision <- 0;
                        water_observed_progress <- workspace_checkpoint;
                    }
                    water_readonly <- true;
                    live_water_file <- workspace_checkpoint + ".water.json";
                    do observe_water;
                    list fs <- state["flowers"];
                    map a <- fs[0]; map b <- fs[1]; map h <- state["slot"];
                    flower_positions <- fs collect map(each)["position"];
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
                    string observation_file <- workspace_checkpoint + ".observations.json";
                    if (file_exists(observation_file)) {
                        map observed <- json_file(observation_file).contents;
                        if (observed["session"] = state["session"]) {
                            int received_sequence <- int(observed["sequence"]);
                            string received_source <- string(observed["source"]);
                            observation_unchanged <- (received_source = world_source and received_sequence = world_sequence) ? observation_unchanged + 1 : 0;
                            world_source <- received_source; world_sequence <- received_sequence;
                            observation_received_ms <- observed contains_key "received_at_ms" ? float(observed["received_at_ms"]) : -1.0;
                            perched_flower_id <- "";
                            world_entities <- [];
                            loop item over: list(observed["entities"]) { map e <- map(item); world_entities[string(e["id"])] <- e; }
                            ask synced_entity where (!(world_entities contains_key each.entity_id)) { do die; }
                            loop key over: world_entities.keys {
                                map e <- map(world_entities[key]);
                                if (empty(synced_entity where (each.entity_id = key))) { create synced_entity with: [entity_id::key]; }
                                ask synced_entity where (each.entity_id = key) {
                                    entity_kind <- string(e["kind"]); entity_position <- point(e["position"]); entity_label <- string(e["label"]);
                                    has_direction <- e contains_key "forward";
                                    entity_forward <- has_direction ? point(e["forward"]) : {0,0,0};
                                }
                            }
                            if (world_entities contains_key "player-bee") {
                                map player <- map(world_entities["player-bee"]);
                                perched_flower_id <- player contains_key "perched_flower_id" ? string(player["perched_flower_id"]) : "";
                            }
                            view_status <- (observation_fresh() ? "LIVE observations | " : "STALE / no recent update | ") + "sequence " + string(world_sequence);
                        }
                    }
                } else { view_status <- "Progress file has an unsupported schema"; }
            } catch { view_status <- "Reading progress; retrying (last complete frame retained)"; }
        } else if (workspace_readonly) { view_status <- "No progress file: choose Progress file in Parameters"; }
    }
    bool observation_fresh {
        return observation_received_ms >= 0 and gama.machine_time >= observation_received_ms
            and gama.machine_time - observation_received_ms < 3000;
    }
    action frame_map {
        water_draw_cells <- !(show_environment and background_ready) and show_salt_grid;
        if (length(flower_positions) >= 2 and !empty(state)) {
            point a <- point(flower_positions[0]); point b <- point(flower_positions[1]);
            point h <- point(slot_coordinates);
            list<point> locations <- (flower_positions collect point(each)) + [h];


            loop entry over: world_entities.values { map e <- map(entry); if (e["kind"] != "bee") { locations << point(e["position"]); } }
            if (show_environment and background_ready) { locations <- locations + background_bounds; }
            if (water_ready) {
                locations << {float(water_origin[0]),float(water_origin[1]),0};
                locations << {float(water_origin[0])+water_width*water_cell_size,float(water_origin[1])+water_height*water_cell_size,0};
            }
            if (!water_ready) { locations <- locations + water_footprint_bounds; }
            float min_x <- min(locations collect each.x); float max_x <- max(locations collect each.x);
            float min_y <- min(locations collect each.y); float max_y <- max(locations collect each.y);
            float span <- max([max_x-min_x,max_y-min_y,4.0]);
            map_left <- min_x-(span-(max_x-min_x))/2;
            map_top <- max_y+(span-(max_y-min_y))/2;
            map_zoom <- 140.0/span;
            water_shared_projection <- true;
            water_map_left <- map_left; water_map_top <- map_top; water_map_zoom <- map_zoom;
            ask water_cell { location <- world.water_display_position(cell_id); }
            ask water_footprint { location <- world.project_world(world_position); }
            map_flowers <- flower_positions collect project_world(point(each));
            // Agent indices keep increasing after deletion; they are not list offsets.
            if (length(pilot_flower) != length(flower_positions)) {
                ask pilot_flower { do die; }
                loop i from: 0 to: length(flower_positions)-1 {
                    create pilot_flower with: [flower_index::i];
                }
            }
            list<point> hive_points <- [];
            loop entry over: world_entities.values { map e <- map(entry); if(e["kind"]="hive") { hive_points << point(e["position"]); } }
            map_hive_center <- empty(hive_points) ? h : {mean(hive_points collect each.x),mean(hive_points collect each.y),mean(hive_points collect each.z)};
            map_slot <- project_world(map_hive_center); map_bee <- project_world(bee_position);
            map_chunk <- project_world(point(state["chunk_pos"]));
            ask pilot_flower { world_position <- point(flower_positions[flower_index]); location <- map_flowers[flower_index]; }
            ask pilot_bee { world_position <- bee_position; location <- map_bee; }
            ask pilot_slot { world_position <- h; location <- map_slot; }
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
                    observation_received_ms <- gama.machine_time;
                    perched_flower_id <- "";
                    if (candidate contains_key "player-bee") {
                        map player <- map(candidate["player-bee"]);
                        perched_flower_id <- player contains_key "perched_flower_id" ? string(player["perched_flower_id"]) : "";
                    }
                    ask synced_entity where (!(world_entities contains_key each.entity_id)) { do die; }
                    loop key over: world_entities.keys {
                        map e <- map(world_entities[key]);
                        list<synced_entity> found <- synced_entity where (each.entity_id = key);
                        if (empty(found)) { create synced_entity with: [entity_id::key]; }
                        ask synced_entity where (each.entity_id = key) {
                            entity_kind <- string(e["kind"]); entity_position <- point(e["position"]);
                            entity_label <- string(e["label"]);
                            has_direction <- e contains_key "forward";
                            entity_forward <- has_direction ? point(e["forward"]) : {0,0,0};
                        }
                    }
                    // Use the server clock: headset clocks can differ or move backwards.
                    if (observation_received_ms - observation_export_time >= 200.0) {
                        save ["session"::state["session"],"source"::source,"sequence"::sequence,
                            "received_at_ms"::observation_received_ms,"entities"::entries]
                            to: checkpoint + ".observations.json" format: "json" rewrite: true;
                        observation_export_time <- observation_received_ms;
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
    // Refilling is game time, not a botanical production rate. Only the server grants nectar.
    reflex refill_nectar when: !workspace_readonly and state contains_key "refill_remaining"
        and (!(state contains_key "refill_enabled") or bool(state["refill_enabled"])) {
        list remaining <- state["refill_remaining"]; list nectar <- state["nectar"];
        bool changed <- false;
        loop i from: 0 to: length(remaining)-1 {
            if (float(remaining[i]) > 0.0) {
                remaining[i] <- max(0.0,float(remaining[i])-0.1);
                if (float(remaining[i]) <= 0.0) { nectar[i] <- 1; changed <- true; }
            }
        }
        state["refill_remaining"] <- remaining; state["nectar"] <- nectar;
        if (changed) { state["revision"] <- int(state["revision"])+1; do persist; }
    }
    action configure(string config_json) {
        if (workspace_readonly) { state <- []; receipts <- []; }
        workspace_readonly <- false;
        has_bee_sample <- true;
        view_status <- "UNITY SERVER | current simulation positions";
        map config <- from_json(config_json);
        checkpoint <- string(config["checkpoint"]);
        string migration_backup <- "";
        if (file_exists(checkpoint)) {
            // json_file buffers can be shared across experiments. Never mutate that cache.
            map saved <- from_json(to_json(json_file(checkpoint).contents));
            state <- saved["state"];
            receipts <- saved["receipts"];
            if (state["schema"] = legacy_pilot_schema) {
                // Capture a deep snapshot before changing only the identity schema.
                migration_backup <- to_json(saved);
                state["schema"] <- pilot_schema;
            }
        }
        if (empty(state)) {
            state <- ["schema"::pilot_schema, "session"::config["session"], "revision"::0,
                "flowers"::config["flowers"], "slot"::config["slot"],
                "contact_radius"::config["contact_radius"], "grab_radius"::config["grab_radius"],
                "snap_radius"::config["snap_radius"], "nectar"::(list(config["flowers"]) collect 1),
                "pollinated"::(list(config["flowers"]) collect false), "pollen_source"::-1, "visits"::0, "last_contact"::-1,
                "chunk_id"::"", "chunk_state"::"none", "chunk_pos"::[0.0,0.0,0.0],
                "stored"::0];
            do persist;
        }
        // Story layout authoring may move existing identities; never reset resources.
        bool allow_layout <- config contains_key "allow_layout_update" and bool(config["allow_layout_update"]);
        if (allow_layout and state["schema"] = pilot_schema and state["session"] = config["session"]) {
            list old_flowers <- state["flowers"]; list new_flowers <- config["flowers"];
            bool same_ids <- length(old_flowers) = length(new_flowers) and length(new_flowers) >= 2;
            same_ids <- same_ids and state["contact_radius"] = config["contact_radius"]
                and state["grab_radius"] = config["grab_radius"] and state["snap_radius"] = config["snap_radius"];
            if (same_ids) {
                loop i from: 0 to: length(old_flowers)-1 {
                    map old_f <- old_flowers[i]; map new_f <- new_flowers[i];
                    same_ids <- same_ids and old_f["id"] = new_f["id"] and old_f["plant_id"] = new_f["plant_id"]
                        and old_f["species"] = new_f["species"] and length(list(new_f["position"])) = 3;
                }
            }
            map old_slot <- state["slot"]; map new_slot <- config["slot"];
            same_ids <- same_ids and old_slot["id"] = new_slot["id"] and length(list(new_slot["position"])) = 3;
            bool old_multi <- state contains_key "hive_slots"; bool new_multi <- config contains_key "hive_slots";
            same_ids <- same_ids and old_multi = new_multi;
            if (same_ids and old_multi) {
                list old_cells <- state["hive_slots"]; list new_cells <- config["hive_slots"];
                same_ids <- length(old_cells) = length(new_cells);
                if (same_ids) {
                    loop i from: 0 to: length(old_cells)-1 {
                        map old_c <- old_cells[i]; map new_c <- new_cells[i];
                        same_ids <- same_ids and old_c["id"] = new_c["id"] and length(list(new_c["position"])) = 3;
                    }
                    map first <- new_cells[0];
                    same_ids <- same_ids and first["id"] = new_slot["id"] and first["position"] = new_slot["position"];
                }
            }
            bool moved <- old_flowers != new_flowers or old_slot != new_slot;
            if (old_multi and new_multi) { moved <- moved or state["hive_slots"] != config["hive_slots"]; }
            if (same_ids and moved) {
                string layout_backup <- checkpoint + ".before-layout-" + string(state["revision"]) + ".json";
                if (!file_exists(layout_backup)) { save ["state"::state,"receipts"::receipts] to: layout_backup format: "json" rewrite: true; }
                state["flowers"] <- new_flowers; state["slot"] <- new_slot;
                if (old_multi) { state["hive_slots"] <- config["hive_slots"]; }
                state["layout_revision"] <- (state contains_key "layout_revision" ? int(state["layout_revision"]) : 0) + 1;
                state["revision"] <- int(state["revision"]) + 1;
                last_contact <- int(state["last_contact"]);
                do persist;
            }
        }
        bool matches <- state["schema"] = pilot_schema and state["session"] = config["session"]
            and state["flowers"] = config["flowers"] and state["slot"] = config["slot"]
            and state["contact_radius"] = config["contact_radius"]
            and state["grab_radius"] = config["grab_radius"] and state["snap_radius"] = config["snap_radius"];
        bool expand_hive <- config contains_key "hive_slots";
        list requested_slots <- expand_hive ? list(config["hive_slots"]) : [];
        if (expand_hive) {
            list<string> ids <- [];
            bool valid_slots <- length(requested_slots) > 0 and length(requested_slots) <= 64;
            loop entry over: requested_slots {
                map cell <- map(entry); string cell_id <- string(cell["id"]);
                valid_slots <- valid_slots and cell_id != "" and !(cell_id in ids) and length(list(cell["position"])) = 3;
                ids << cell_id;
            }
            if (!empty(requested_slots)) {
                map first_cell <- requested_slots[0]; map old_cell <- state["slot"];
                valid_slots <- valid_slots and first_cell["id"] = old_cell["id"] and first_cell["position"] = old_cell["position"];
            }
            matches <- matches and valid_slots;
            if (state contains_key "hive_slots") { matches <- matches and state["hive_slots"] = requested_slots; }
        } else if (state contains_key "hive_slots") { matches <- false; }
        if (matches and expand_hive and !(state contains_key "hive_slots")) {
            // The old occupied cell becomes the first occupied cell; keep all receipts and pollen.
            string full_backup <- checkpoint + ".before-full-hive.json";
            if (!file_exists(full_backup)) { save ["state"::state,"receipts"::receipts] to: full_backup format: "json" rewrite: true; }
            state["hive_slots"] <- requested_slots;
            state["hive_capacity"] <- length(requested_slots);
            state["filled_slots"] <- int(state["stored"]) > 0 ? ["slot-a"] : [];
            state["deposited_slot"] <- state["chunk_state"] = "stored" ? "slot-a" : "";
            state["refill_seconds"] <- max(1.0,config contains_key "nectar_refill_seconds" ? float(config["nectar_refill_seconds"]) : 8.0);
            list old_nectar <- state["nectar"];
            state["refill_remaining"] <- (old_nectar collect (int(each) = 0 ? state["refill_seconds"] : 0.0));
            last_contact <- int(state["last_contact"]);
            do persist;
        }
        response <- ["event_id"::"configure", "accepted"::matches,
            "reason"::(matches ? "ready" : "session_or_geometry_mismatch"), "migrated"::(matches and migration_backup != "")];
        if (matches) {
            if (config contains_key "nectar_refill_enabled") {
                bool refill_enabled <- bool(config["nectar_refill_enabled"]);
                if (!(state contains_key "refill_enabled") or state["refill_enabled"] != refill_enabled) {
                    state["refill_enabled"] <- refill_enabled;
                    last_contact <- int(state["last_contact"]);
                    do persist;
                }
            }
            if (migration_backup != "") {
                string backup_path <- checkpoint + ".before-letitbee.json";
                if (!file_exists(backup_path)) {
                    save from_json(migration_backup) to: backup_path format: "json" rewrite: true;
                }
                last_contact <- int(state["last_contact"]);
                do persist;
            }
            string source <- config contains_key "world_source" ? string(config["world_source"]) : "";
            if (source != world_source) {
                world_source <- source; world_sequence <- -1; world_signature <- "";
                observation_export_time <- 0.0; observation_received_ms <- -1.0; perched_flower_id <- "";
                world_entities <- []; ask synced_entity { do die; }
                world_response <- ["accepted"::false,"reason"::"awaiting_world","source"::world_source,"sequence"::world_sequence];
            }
        }
        last_contact <- int(state["last_contact"]);
        list configured_flowers <- state["flowers"];
        flower_positions <- configured_flowers collect map(each)["position"];
        nectar_positions <- [];
        flower_surfaces <- []; flower_radii <- [];
        map configured_slot <- state["slot"];
        slot_coordinates <- configured_slot["position"];
        live_hive_slots <- state contains_key "hive_slots" ? list(state["hive_slots"]) : [];
        // Environmental state is separate from honey receipts and progress.
        water_checkpoint <- checkpoint + ".water.json"; water_readonly <- false;
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
        if (pose contains_key "hive_slots" and pose["hive_slots"] != nil and state contains_key "hive_slots") {
            list updated <- list(pose["hive_slots"]); list expected <- state["hive_slots"];
            bool valid <- length(updated) = length(expected);
            if (valid) {
                loop i from: 0 to: length(expected)-1 {
                    map a <- updated[i]; map b <- expected[i];
                    valid <- valid and a["id"] = b["id"] and length(list(a["position"])) = 3;
                }
            }
            if (valid) { live_hive_slots <- updated; }
        }
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
                            bool full_mode <- state contains_key "hive_capacity";
                            int capacity <- full_mode ? int(state["hive_capacity"]) : 1;
                            bool available <- state["chunk_state"] = "none" or (full_mode and state["chunk_state"] = "stored");
                            if (available and int(nectar[fi]) > 0 and int(state["stored"]) < capacity and (!full_mode or last_contact = fi)) {
                                nectar[fi] <- int(nectar[fi]) - 1;
                                state["nectar"] <- nectar;
                                state["chunk_id"] <- "nectar-" + eid;
                                state["chunk_state"] <- "ready";
                                state["chunk_pos"] <- length(nectar_positions) > fi ? nectar_positions[fi] : flower_positions[fi];
                                state["chunk_flower"] <- fi;
                                if (full_mode) {
                                    list refill <- state["refill_remaining"]; refill[fi] <- state["refill_seconds"];
                                    state["refill_remaining"] <- refill; state["deposited_slot"] <- "";
                                }
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
                        if (state contains_key "hive_capacity") {
                            if (r contains_key "drop_target") {
                                point landing <- point(r["drop_target"]);
                                if (abs(landing.x-held_chunk_position.x)<0.1 and abs(landing.y-held_chunk_position.y)<0.1
                                    and landing.z<=held_chunk_position.z+0.1 and landing.z>=held_chunk_position.z-2.1) {
                                    state["chunk_pos"] <- [landing.x,landing.y,landing.z];
                                }
                            } else if (r contains_key "recover_drop" and bool(r["recover_drop"])) {
                                int origin <- int(state["chunk_flower"]);
                                state["chunk_state"] <- "ready";
                                state["chunk_pos"] <- length(nectar_positions)>origin ? nectar_positions[origin] : flower_positions[origin];
                            }
                        }
                        accepted <- true;
                        reason <- "dropped";
                    }
                }
                if (kind = "deposit") {
                    reason <- "wrong_chunk_slot_or_distance";
                    map slot <- state["slot"];
                    point snap_point <- point(slot_coordinates);
                    bool free_slot <- int(state["stored"]) = 0;
                    if (state contains_key "hive_slots") {
                        free_slot <- false;
                        loop entry over: live_hive_slots {
                            map cell <- map(entry);
                            if (cell["id"] = r["slot_id"]) {
                                slot <- cell; snap_point <- point(cell["position"]);
                                list filled <- state["filled_slots"]; free_slot <- !(r["slot_id"] in filled);
                            }
                        }
                    }
                    if (state["chunk_state"] = "held" and r["chunk_id"] = state["chunk_id"]
                        and r["slot_id"] = slot["id"] and free_slot
                        and held_chunk_valid
                        and (held_chunk_position distance_to snap_point) <= float(state["snap_radius"])
                        and (bee_position distance_to snap_point) <= 2.0) {
                        state["stored"] <- int(state["stored"])+1;
                        if (state contains_key "filled_slots") {
                            list filled <- state["filled_slots"]; filled << r["slot_id"];
                            state["filled_slots"] <- filled; state["deposited_slot"] <- r["slot_id"];
                        }
                        state["chunk_state"] <- "stored";
                        state["chunk_pos"] <- [snap_point.x,snap_point.y,snap_point.z];
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
    point world_position;
    int flower_index;
    aspect default {
        if (!empty(state) and world_sequence < 0) {
            list flowers <- state["flowers"]; map f <- flowers[flower_index];
            list ps <- state["pollinated"];
            draw circle(1.5) at: map_flowers[flower_index] color: (bool(ps[flower_index]) ? #green : #pink) border: #black;
            draw circle(0.55) at: map_flowers[flower_index]+{2.1,0}
                color: (int(list(state["nectar"])[flower_index]) > 0 ? #orange : #white) border: #orange;
            draw ("Flower " + string(flower_index + 1)) at: map_flowers[flower_index]+(flower_index = 0 ? {0,8} : {0,-8}) color: #black font: font("SansSerif",14);
        }
    }
}
species pilot_bee {
    point world_position;
    aspect default {
        if (has_bee_sample and world_sequence < 0) {
            draw circle(2) at: map_bee color: #gray border: #black;
        }
    }
}
species pilot_slot {
    point world_position;
    aspect default {
        if (!empty(state)) {
            if (true) {
                draw hexagon(2.2) at: map_slot color: (int(state["stored"]) > 0 ? #green : #orange) border: #black;
                draw "Hive " + string(state["stored"]) + "/" + string(state contains_key "hive_capacity" ? state["hive_capacity"] : 1) at: map_slot+{5,0} color: #black font: font("SansSerif",11);
            }
            if (state["chunk_state"] = "ready" or state["chunk_state"] = "loose") {
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
    point entity_forward <- {0,0,0};
    bool has_direction <- false;
    aspect default {
        if (entity_kind = "bee") {
            rgb tint <- world.observation_fresh() ? #yellow : #gray;
            point d <- {entity_forward.x, -entity_forward.y, 0};
            float size <- sqrt(d.x*d.x+d.y*d.y);
            if (has_direction and size > 0.001) {
                d <- d / size;
                point side <- {-d.y,d.x,0};
                draw polygon([location+d*4,location-d*2+side*2,location-d*2-side*2]) color: tint border: #black;
            } else { draw circle(2) color: tint border: #black; }
        }
        if (entity_kind = "flower") {
            int fi <- -1;
            list configured <- state["flowers"];
            loop i from: 0 to: length(configured)-1 { map f <- configured[i]; if (f["id"] = entity_id) { fi <- i; } }
            bool received <- fi >= 0 ? bool(list(state["pollinated"])[fi]) : false;
            draw circle(1.5) color: (fi < 0 ? #gray : (received ? #green : #pink)) border: #black;
            if (fi >= 0) {
                bool nectar_available <- int(list(state["nectar"])[fi]) > 0;
                draw circle(0.55) at: location+{2.1,0} color: (nectar_available ? #orange : #white) border: #orange;
                draw string(fi+1) at: location+{0,3} color: #black font: font("SansSerif",11);
            }
            if (world.observation_fresh() and entity_id = perched_flower_id) {
                draw (circle(2.2) - circle(2.0)) color: #cyan;
            }
        }
        // Hive slots retain their real coordinates; pilot_slot draws one aggregate marker.
    }
}
species water_footprint {
    point world_position;
    float cell_size;
    aspect default {
        if (!water_ready and !(show_environment and background_ready)) { draw square(cell_size*map_zoom) color: #gray; }
    }
}
species observation_tile {
    point world_position;
    point world_size;
    bool is_water;
    aspect default {
        if (show_environment and background_ready) {
            rgb tint <- is_water ? rgb(156,195,201) : rgb(218,222,177);
            if (is_water and show_salt_grid and water_ready) {
                int col <- int(floor((world_position.x-float(water_origin[0]))/water_cell_size));
                int row <- int(floor((world_position.y-float(water_origin[1]))/water_cell_size));
                if(col>=0 and col<water_width and row>=0 and row<water_height) {
                    int id <- row*water_width+col;
                    if(water_volume[id]>0) {
                        float sal <- water_salt[id]/water_volume[id];
                        tint <- water_readonly and !water_view_fresh ? #gray : rgb(int(30+220*min(1.0,sal/35.0)),int(170-100*min(1.0,sal/35.0)),200);
                    }
                }
            }
            draw rectangle(world_size.x*map_zoom,world_size.y*map_zoom) at: world.project_world(world_position) color: tint;
        }
    }
}
species observation_landmark {
    point world_position;
    point world_size;
    string landmark_kind;
    string landmark_label;
    aspect default {
        if(show_environment and background_ready) {
            point p <- world.project_world(world_position);
            if(landmark_kind="house") {
                draw rectangle(world_size.x*map_zoom,world_size.y*map_zoom) at: p color: rgb(152,119,84) border: #brown;
            } else {
                float radius <- min(1.6,max(0.5,world_size.x*map_zoom*0.15));
                draw circle(radius) at:p color: (landmark_kind="nipa" ? rgb(149,154,63) : rgb(87,133,64));
                draw circle(0.2) at:p color: #brown;
            }
        }
    }
}
species pilot_legend {
    aspect default {
        draw view_status at: {6,7} color: #black font: font("SansSerif",13);
        draw ("Background: "+background_status) at: {6,22} color: #black font: font("SansSerif",9);
        draw "N (+Unity Z)" at: {169,28} color: #black font: font("SansSerif",9);
        draw ("Water: "+water_view_status) at: {6,15} color: #black font: font("SansSerif",11);
        if (!water_ready and !empty(water_footprint_bounds)) {
            draw "Gray water: survey footprint only | salinity NOT available" at: {6,181} color: #black font: font("SansSerif",10);
        }
        draw "Land: beige | Nipa: olive | Palm: green | House: brown" at: {6,169} color: #black font: font("SansSerif",9);
        draw "Blue-gray shore: geometry only, outside salt cells | Background is an export" at: {6,173} color: #black font: font("SansSerif",9);
        if (water_ready and show_salt_grid) {
            loop i from: 0 to: 7 {
                draw rectangle(8,2) at: {10+i*8,177} color: rgb(int(30+220*i/7.0),int(170-100*i/7.0),200);
            }
            draw "0                 Salinity (g/L)                 35+ | Gray: stale" at: {6,181} color: #black font: font("SansSerif",10);
        }
        draw "Pink: no pollen | Green: pollinated | Cyan ring: perched" at: {6,188} color: #black font: font("SansSerif",12);
        if (workspace_readonly and world_sequence < 0) {
            draw "Checkpoint view; bee movement between transactions is not streamed" at: {6,195} color: #black font: font("SansSerif",12);
        }
        if (world_sequence >= 0) {
            draw ("Nectar dot: orange = available, white = collected | Bee gray = stale") at: {6,195} color: #black font: font("SansSerif",12);
        }
    }
}
experiment HoneyPilot type: gui {
    permanent { }
    float minimum_cycle_duration <- 0.1;
    parameter "Progress file (workspace read-only view)" var: workspace_checkpoint;
    parameter "Water footprint survey (geometry only)" var: water_footprint_file;
    parameter "Environment map JSON" var: observation_map_file;
    parameter "Show environment background" var: show_environment;
    parameter "Show salinity layer" var: show_salt_grid;
    parameter "Inspect water cell index (-1 = none)" var: inspected_water_cell min: -1 max: 4095;
    output {
        display HoneyPilotMap type: java2D background: #lightgray {
            species observation_tile; species water_footprint;
            species water_cell;
            species observation_landmark; species pilot_flower; species pilot_slot; species pilot_bee; species synced_entity; species pilot_legend;
        }
        monitor "Water status" value: water_view_status;
        monitor "Water checkpoint" value: live_water_file;
        monitor "Water time (s)" value: water_time_seconds;
        monitor "Water revision" value: water_revision;
        monitor "Selected cell salinity (g/L; check status)" value: (water_ready and inspected_water_cell >= 0 and inspected_water_cell < length(water_volume) ? (water_volume[inspected_water_cell] > 0 ? string(water_salt[inspected_water_cell]/water_volume[inspected_water_cell]) : "Dry cell") : "No cell data");
        monitor "Selected cell Unity X,Z" value: (water_ready and inspected_water_cell >= 0 and inspected_water_cell < length(water_volume) ? string({float(water_origin[0])+(inspected_water_cell mod water_width+0.5)*water_cell_size,float(water_origin[1])+(int(inspected_water_cell/water_width)+0.5)*water_cell_size,0}) : "No cell data");
        monitor "View mode" value: view_status;
        monitor "Pollinated flowers" value: state["pollinated"];
        monitor "Nectar remaining per flower" value: state["nectar"];
        monitor "Honey in hive" value: string(state["stored"]) + "/" + string(state["hive_capacity"]);
        monitor "State" value: state;
        monitor "Last transaction" value: response;
        monitor "World sync" value: world_response;
    }
}
