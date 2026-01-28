// Further possible enhancements;
//
// Particle birds, forest ambients, soundscapes and sunrays
// water surfaces can have moss
// Textures of the map can be modified
// Roof detecting
// Enhancement of map compatibility

using Sandbox;
using Sandbox.Diagnostics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static Sandbox.Component;

public partial class PAF_Entity : Component, Component.IPressable, Component.INetworkSpawn
{
	private Logger Logger = new( "PAF Entity" );

	//Files
	[Property, Group( "Files" )] Texture forestFrame { get; set; }
	[Property, Group( "Files" )] Texture threeTrees { get; set; }

	public PAF_Entity()
	{
		Logger.Info( "[sbox.community] Post Apocalyptic Forestation addon is loaded!" );
	}

	protected override void OnDestroy()
	{
		if ( !IsProxy )
			PostApocalypticForestation.PAF_Clear();

		base.OnDestroy();
	}


	public bool Press( IPressable.Event e )
	{
		if ( IsProxy )
			return false;

		Sound.Play( "switch_006", GameObject.WorldPosition );

		using ( Rpc.FilterInclude( c => c == Connection.Local ) )
		{
			PostApocalypticForestation.adminCommandHandler( 0 );
		}

		return true;
	}
	public bool CanPress( IPressable.Event e )
	{
		return true;
	}
	public void OnNetworkSpawn( Connection owner )
	{
		Network.AssignOwnership( Rpc.Caller );
	}
}

public partial class PostApocalypticForestation : INetworkListener
{
	private static readonly bool _debug = false;
	public static Logger Logger = new( "PAF" );

	public static bool Generated = false;
	public static float GenerateSpamProtection = 0f;
	public static CancellationTokenSource Cancelled = new();

	static List<GameObject> spawnedTrees = null; //svside
	static List<GameObject> spawnedRocks = null; //svside
	static List<GameObject> spawnedGrasses = null; //clside
	static List<GameObject> spawnedIvys = null; //clside
	static List<GameObject> spawnedDebris = null; //clside

	static List<EntInfo> doneGrasses = new();
	static List<EntInfo> doneIvys = new();
	static List<EntInfo> doneDebris = new();

	static Dictionary<long, Connection> spamProtect = new();
	static bool firstCalculation = false;

	static Menu mainPanel;
	static ScreenPanel screenPanelComponent;

	static BBox calculatedBounds = new();
	static float calculatedHeight = 0f;

	// Convars saving not working?
	[ConVar( "sv_paf_treeamount", Help = "Maximum tree amount of post apocalyptic forestation", Max = 5000, Min = 0, Saved = true, Flags = ConVarFlags.Replicated )]
	public static int treeAmount { get; set; } = 1000;
	[ConVar( "sv_paf_rockamount", Help = "Maximum rock amount of post apocalyptic forestation", Max = 500, Min = 0, Saved = true, Flags = ConVarFlags.Replicated )]
	public static int rockAmount { get; set; } = 200;
	[ConVar( "sv_paf_grassamount", Help = "Maximum grass amount of post apocalyptic forestation", Max = 6000, Min = 0, Saved = true, Flags = ConVarFlags.Replicated )]
	public static int grassAmount { get; set; } = 5000;
	[ConVar( "sv_paf_ivyamount", Help = "Maximum ivy amount of post apocalyptic forestation", Max = 3000, Min = 0, Saved = true, Flags = ConVarFlags.Replicated )]
	public static int ivyAmount { get; set; } = 2000;
	[ConVar( "sv_paf_debrisamount", Help = "Maximum debris amount of post apocalyptic forestation", Max = 1000, Min = 0, Saved = true, Flags = ConVarFlags.Replicated )]
	public static int debrisAmount { get; set; } = 1000;

	static readonly string[] TreeModels = new string[]
	{
		"rust.creeping_tree_a",
		"rust.creeping_tree_b",
		"rust.creeping_tree_c",
		"rust.creeping_tree_d",
		"rust.creeping_tree_e",
		"rust.creeping_tree_f",
		"rust.creeping_tree_g",
		"rust.creeping_tree_conifer_a",
		"rust.american_beech_a",
		"rust.american_beech_a_dead",
		"rust.american_beech_b",
		"rust.american_beech_c",
		"rust.american_beech_d",
		"rust.american_beech_d_dead",
		"rust.american_beech_e",
		"rust.american_beech_e_dead",
	//"models/rust_nature/overgrowth/creeping_tree_a.vmdl",
	//"models/rust_nature/overgrowth/creeping_tree_b.vmdl",
	//"models/rust_nature/overgrowth/creeping_tree_c.vmdl",
	//"models/rust_nature/overgrowth/creeping_tree_d.vmdl",
	//"models/rust_nature/overgrowth/creeping_tree_e.vmdl",
	//"models/rust_nature/overgrowth/creeping_tree_f.vmdl",
	//"models/rust_nature/overgrowth/creeping_tree_g.vmdl",
	//"models/rust_nature/overgrowth/creeping_tree_conifer_a.vmdl",
	//"models/sbox_props/trees/horse_chestnut/horse_chestnut.vmdl", //removed
	//"models/sbox_props/trees/oak/tree_oak_big_a.vmdl", //removed
	//"models/sbox_props/trees/oak/tree_oak_big_b.vmdl", //removed
	//"models/sbox_props/trees/oak/tree_oak_medium_a.vmdl", //removed
	//"models/sbox_props/trees/oak/tree_oak_small_a.vmdl", //removed
	//"models/rust_nature/american_beech/american_beech_a.vmdl",
	//"models/rust_nature/american_beech/american_beech_a_dead.vmdl",
	//"models/rust_nature/american_beech/american_beech_b.vmdl",
	//"models/rust_nature/american_beech/american_beech_c.vmdl",
	//"models/rust_nature/american_beech/american_beech_d.vmdl",
	//"models/rust_nature/american_beech/american_beech_d_dead.vmdl",
	//"models/rust_nature/american_beech/american_beech_e.vmdl",
	//"models/rust_nature/american_beech/american_beech_e_dead.vmdl"
	};

	static readonly string[] GrassModels = new string[]
	{
		"rust.patch_grass_small",
		"rust.patch_grass_short",
		"rust.patch_grass_medium",
		"rust.cracks_grass_a",
		"rust.cracks_grass_b",
		"rust.cracks_grass_c",
		"rust.cracks_grass_d",
		"rust.cracks_grass_e",
		"rust.cracks_grass_f",
		"rust.cracks_grass_g",
		"rust.cracks_grass_h",
		"rust.cracks_grass_i",
		"rust.cracks_grass_j",
		"rust.patch_grass_tall",
		"rust.patch_grass_tall_large"
	//"models/sbox_props/nature/grass_clumps/grass_clump_a.vmdl",
	//"models/rust_nature/overgrowth/patch_grass_small.vmdl",
	//"models/rust_nature/overgrowth/patch_grass_short.vmdl",
	//"models/rust_nature/overgrowth/patch_grass_medium.vmdl",
	//"models/rust_nature/overgrowth/cracks_grass_a.vmdl",
	//"models/rust_nature/overgrowth/cracks_grass_b.vmdl",
	//"models/rust_nature/overgrowth/cracks_grass_c.vmdl",
	//"models/rust_nature/overgrowth/cracks_grass_d.vmdl",
	//"models/rust_nature/overgrowth/cracks_grass_e.vmdl",
	//"models/rust_nature/overgrowth/cracks_grass_f.vmdl",
	//"models/rust_nature/overgrowth/cracks_grass_g.vmdl",
	//"models/rust_nature/overgrowth/cracks_grass_h.vmdl",
	//"models/rust_nature/overgrowth/cracks_grass_i.vmdl",
	//"models/rust_nature/overgrowth/cracks_grass_j.vmdl",
	//"models/rust_nature/overgrowth/patch_grass_tall.vmdl",
	//"models/rust_nature/overgrowth/patch_grass_tall_large.vmdl",
	//"models/sbox_props/nature/grass_clumps/grass_clump_a.vmdl",
	//"models/sbox_props/nature/grass_clumps/grass_clump_b.vmdl",
	//"models/sbox_props/nature/grass_clumps/grass_clump_c.vmdl"
	};

	static readonly string[] BushModels = new string[]
	{
		"models/sbox_props/shrubs/beech/beech_bush_large.vmdl_c",
		//"models/sbox_props/shrubs/beech/beech_hedge_40x128.vmdl_c",
		//"models/sbox_props/shrubs/beech/beech_hedge_40x128_corner.vmdl_c",
		//"models/sbox_props/shrubs/beech/beech_hedge_96x64_blend_up.vmdl_c"
	//"models/sbox_props/shrubs/beech/beech_bush_large.vmdl",
	//"models/sbox_props/shrubs/beech/beech_bush_medium.vmdl",
	//"models/sbox_props/shrubs/beech/beech_bush_medium_wall_corner.vmdl",
	//"models/sbox_props/shrubs/beech/beech_bush_regular_medium_a.vmdl",
	//"models/sbox_props/shrubs/beech/beech_bush_regular_medium_b.vmdl",
	//"models/sbox_props/shrubs/beech/beech_bush_regular_small.vmdl",
	//"models/sbox_props/shrubs/beech/beech_bush_small.vmdl",
	//"models/sbox_props/shrubs/beech/beech_shrub_tall_large.vmdl",
	//"models/sbox_props/shrubs/beech/beech_shrub_tall_medium.vmdl",
	//"models/sbox_props/shrubs/beech/beech_shrub_tall_small.vmdl",
	//"models/sbox_props/shrubs/beech/beech_shrub_wide_large.vmdl",
	//"models/sbox_props/shrubs/beech/beech_shrub_wide_medium.vmdl",
	//"models/sbox_props/shrubs/beech/beech_shrub_wide_small.vmdl"
	};

	static readonly string[] FlowerModels = new string[]
	{
		"rust.patch_flowers_a",
		"rust.patch_flowers_b",
		"rust.patch_flowers_c",
		"rust.patch_flowers_d"
	//"models/rust_nature/overgrowth/patch_flowers_a.vmdl",
	//"models/rust_nature/overgrowth/patch_flowers_b.vmdl",
	//"models/rust_nature/overgrowth/patch_flowers_c.vmdl",
	//"models/rust_nature/overgrowth/patch_flowers_d.vmdl"
	};

	static readonly string[] ReedModels = new string[]
	{
		"rust.reed_medium",
		"rust.reed_medium_blossom",
		"rust.reed_medium_dry",
		"rust.reed_small",
		"rust.reed_small_dry",
		"rust.reed_smaller",
		"rust.reed_tall_blossom",
		"rust.reeds_dead",
		"rust.reeds_medium",
		"rust.reeds_small",
		"rust.reeds_small_dry",
		"rust.reeds_small_sparse",
		"rust.reeds_tall",
		"rust.reeds_tall_dry"
	//"models/rust_nature/reeds/reed_medium.vmdl",
	//"models/rust_nature/reeds/reed_medium_blossom.vmdl",
	//"models/rust_nature/reeds/reed_medium_dry.vmdl",
	//"models/rust_nature/reeds/reed_small.vmdl",
	//"models/rust_nature/reeds/reed_small_dry.vmdl",
	//"models/rust_nature/reeds/reed_smaller.vmdl",
	//"models/rust_nature/reeds/reed_tall_blossom.vmdl",
	//"models/rust_nature/reeds/reeds_dead.vmdl",
	//"models/rust_nature/reeds/reeds_medium.vmdl",
	//"models/rust_nature/reeds/reeds_small.vmdl",
	//"models/rust_nature/reeds/reeds_small_dry.vmdl",
	//"models/rust_nature/reeds/reeds_small_sparse.vmdl",
	//"models/rust_nature/reeds/reeds_tall.vmdl",
	//"models/rust_nature/reeds/reeds_tall_dry.vmdl",
	};

	static readonly string[] IvyModels = new string[]
	{
		"rust.creeping_plant_a",
		"rust.creeping_plant_b",
		"rust.creeping_plant_fall_a",
		"rust.creeping_plant_fence_600",
		"rust.creeping_plant_wall_600",
		"rust.creeping_plant_wall_900",
		"rust.creeping_plant_corner_a",
		"rust.creeping_plant_corner_b",
		"rust.creeping_plant_corner_c",
		"rust.creeping_plant_pipe_100x600",
		"rust.creeping_plant_pipe_200x600",
		"rust.creeping_plant_pipe_300x600"
	//"models/rust_nature/overgrowth/creeping_plant_a.vmdl",
	//"models/rust_nature/overgrowth/creeping_plant_b.vmdl",
	//"models/rust_nature/overgrowth/creeping_plant_fall_a.vmdl",
	//"models/rust_nature/overgrowth/creeping_plant_fence_600.vmdl",
	//"models/rust_nature/overgrowth/creeping_plant_wall_600.vmdl",
	//"models/rust_nature/overgrowth/creeping_plant_wall_900.vmdl"
		//"models/rust_nature/overgrowth/creeping_plant_corner_a.vmdl",	   
		//"models/rust_nature/overgrowth/creeping_plant_corner_b.vmdl",	   
		//"models/rust_nature/overgrowth/creeping_plant_corner_c.vmdl",	   
		//"models/rust_nature/overgrowth/creeping_plant_pipe_100x600.vmdl",
		//"models/rust_nature/overgrowth/creeping_plant_pipe_200x600.vmdl",
		//"models/rust_nature/overgrowth/creeping_plant_pipe_300x600.vmdl" 

	};

	static readonly string[] RockModels = new string[]
	{

		"rust.rock_cliff_a",
		"rust.rock_cliff_b",
		"rust.rock_cliff_c",
		"rust.rock_ledge_a",
		"rust.rock_med_a",
		"rust.rock_med_b",
		"rust.rock_med_c",
		"rust.rock_quarry_a",
		"rust.rock_quarry_small_a",
		"rust.rock_quarry_small_b",
		"rust.rock_small_a",
		"rust.rock_small_a_cave",
		"rust.rock_small_b",
		"rust.rock_small_b_cave",
		"rust.rock_small_c",
		"rust.rock_small_c_cave"
	//"models/rust_nature/rocks/rock_cliff_a.vmdl",
	//"models/rust_nature/rocks/rock_cliff_b.vmdl",
	//"models/rust_nature/rocks/rock_cliff_c.vmdl",
	//"models/rust_nature/rocks/rock_ledge_a.vmdl",
	//"models/rust_nature/rocks/rock_med_a.vmdl",
	//"models/rust_nature/rocks/rock_med_b.vmdl",
	//"models/rust_nature/rocks/rock_med_c.vmdl",
	//"models/rust_nature/rocks/rock_quarry_a.vmdl",
	//"models/rust_nature/rocks/rock_quarry_small_a.vmdl",
	//"models/rust_nature/rocks/rock_quarry_small_b.vmdl",
	//"models/rust_nature/rocks/rock_small_a.vmdl",
	//"models/rust_nature/rocks/rock_small_a_cave.vmdl",
	//"models/rust_nature/rocks/rock_small_b.vmdl",
	//"models/rust_nature/rocks/rock_small_b_cave.vmdl",
	//"models/rust_nature/rocks/rock_small_c.vmdl",
	//"models/rust_nature/rocks/rock_small_c_cave.vmdl"
	};

	static readonly string[] DebrisModels = new string[]
	{
		"rust.concrete_debris_a",
		"rust.concrete_debris_b",
		"rust.bottle_cluster_a",
		"rust.bottle_cluster_b",
		"rust.bottle_cluster_c",
		"rust.bottle_cluster_d",
		"rust.can_cluster_a",
		"rust.can_cluster_b",
		"rust.cola_can_cluster_a",
		"rust.crisps_cluster_a",
		"rust.ground_cloth_a",
		"rust.ground_junk_a",
		"rust.ground_junk_straight_a",
		"rust.jar_cluster_a",
		"rust.jar_cluster_b",
		"rust.concrete_debris_c",
		"rust.milk_carton_cluster_a"
	//"models/rust_props/concrete_debris/concrete_debris_a.vmdl",
	//"models/rust_props/concrete_debris/concrete_debris_b.vmdl",
	//"models/rust_props/small_junk/bottle_cluster_a.vmdl",
	//"models/rust_props/small_junk/bottle_cluster_b.vmdl",
	//"models/rust_props/small_junk/bottle_cluster_c.vmdl",
	//"models/rust_props/small_junk/bottle_cluster_d.vmdl",
	//"models/rust_props/small_junk/can_cluster_a.vmdl",
	//"models/rust_props/small_junk/can_cluster_b.vmdl",
	//"models/rust_props/small_junk/cola_can_cluster_a.vmdl",
	//"models/rust_props/small_junk/crisps_cluster_a.vmdl",
	//"models/rust_props/small_junk/ground_cloth_a.vmdl",
	//"models/rust_props/small_junk/ground_junk_a.vmdl",
	//"models/rust_props/small_junk/ground_junk_straight_a.vmdl",
	//"models/rust_props/small_junk/jar_cluster_a.vmdl",
	//"models/rust_props/small_junk/jar_cluster_b.vmdl"
		//"models/rust_props/concrete_debris/concrete_debris_c.vmdl",
		//"models/rust_props/small_junk/milk_carton_cluster_a.vmdl",
	};

	struct EntInfo
	{
		public string Model;
		public float Scale;
		public Vector3 Position;
		public Rotation Rotation;
		public Color RenderColor;
	}

	[Rpc.Broadcast]
	public static async void adminCommandHandler( int flag )
	{
		if ( flag == 0 )
		{
			using ( Rpc.FilterInclude( c => c == Rpc.Caller ) )
			{
				openPAFMenu();
			}
		}
		else if ( flag == 1 )
		{
			if ( !Rpc.Caller.CanRefreshObjects )
			{
				InfoClient( "You do not have permission to use this, 'Refresh Objects' permission is needed!" );
				return;
			}

			await PAF_Generate();

		}
		else if ( flag == 2 )
		{
			if ( !Rpc.Caller.CanRefreshObjects )
			{
				InfoClient( "You do not have permission to use this, 'Refresh Objects' permission is needed!" );
				return;
			}

			PAF_Clear();
		}
	}

	public static void removePanel()
	{
		if ( mainPanel != null )
		{
			if ( mainPanel.IsValid() )
				mainPanel.Destroy();

			mainPanel = null;
		}

		if ( screenPanelComponent.IsValid() && screenPanelComponent.GameObject.IsValid() )
		{
			screenPanelComponent.GameObject.Destroy();
			screenPanelComponent = null;
		}
	}
	[Rpc.Broadcast]
	public static void openPAFMenu()
	{
		removePanel();

		GameObject UIGO = new();
		var UI = UIGO.AddComponent<ScreenPanel>();
		mainPanel = UI.GetComponent<ScreenPanel>().AddComponent<Menu>();
		mainPanel.CreateMenu();
	}

	private static void applyEffects()
	{
		var paf_entity = Game.ActiveScene.GetAllObjects( true ).FirstOrDefault( x => x.Name == "paf_entity" && !x.IsProxy );

		if ( paf_entity.IsValid() && paf_entity.GetComponent<GradientFog>( true ) is GradientFog paf_entity_fog_component && paf_entity_fog_component.IsValid() )
		{
			paf_entity_fog_component.Enabled = true;
			paf_entity_fog_component.Color = "#4c5610";
			//paf_entity_fog_component.MaximumOpacity = 0.05f;
			//paf_entity_fog_component.StartHeight = 10;
			paf_entity_fog_component.Height = 200;
			paf_entity_fog_component.FalloffExponent = 10;
			paf_entity_fog_component.VerticalFalloffExponent = 2;
			paf_entity_fog_component.StartDistance = 500;
			paf_entity_fog_component.EndDistance = 3000;

		}

		if ( paf_entity.IsValid() && paf_entity.GetComponent<AmbientLight>( true ) is AmbientLight paf_entity_color_grading_component && paf_entity_color_grading_component.IsValid() )
		{
			paf_entity_color_grading_component.Enabled = true;
			paf_entity_color_grading_component.Color = Color.TryParse( "#4c5610", out var color ) ? color : Color.White;
		}

		//Map.Camera.BackgroundColor = "#4c5610";
	}

	//[ConCmd( "sv_paf_createeffects", Flags = ConVarFlags.Admin )]
	public static void CreateEffects()
	{
		CreateEffectsCL();
	}

	[Rpc.Broadcast]
	public static void CreateEffectsCL()
	{
		if ( Cancelled.IsCancellationRequested )
		{
			PAF_Clear();
			return;
		}
		applyEffects();
	}

	private static async Task SpawnGrasses_thread( int maxray, int amount, List<Vector3> hitpositions, List<Vector3> hitnormals )
	{
		var founded = 0;
		int delaystep = 500; //batch
		int delaycount = 0;

		for ( int i = 0; i < maxray; i++ )
		{
			if ( delaycount == delaystep )
			{
				await GameTask.Delay( 10 );
				delaycount = 0;

				if ( _debug )
					Logger.Info( $"Checked {i}. points" );
			}

			delaycount++;

			if ( founded >= amount )
				break;

			var randomPoint = calculatedBounds.RandomPointInside;

			var angle = Vector3.Down * calculatedHeight;
			angle += randomPoint;

			//Ground checking
			var tr = Game.ActiveScene.Trace.Ray( randomPoint, angle )
				//.UseRenderMeshes( true )
				.UsePhysicsWorld( true )
				.UseHitboxes( false )
				.UseHitPosition( true )
				.WithoutTags( "<invalid>", "water", "paf" )
				.Run();

			if ( !tr.Hit )
				continue;

			//Angles checking for spawning horizontally
			if ( tr.Normal.Angle( Vector3.Up ) > 25.0f )
				continue;

			//Distance checking between them
			if ( spawnedGrasses.Any( x => x.WorldPosition.DistanceSquared( tr.HitPosition ) < 5 * 5 ) )
				continue;

			hitpositions.Add( tr.HitPosition );
			hitnormals.Add( tr.Normal );

			++founded;
		}
	}

	//[ConCmd( "sv_paf_spawngrassess", Flags = ConVarFlags.Admin )]
	public static async Task SpawnGrasses()
	{
		spawnedGrasses ??= new();

		clearForestCL( grasses: true );

		spawnedGrasses.Clear();
		doneGrasses.Clear();

		List<Vector3> hitpositions = new();
		List<Vector3> hitnormals = new();

		//await GameTask.RunInThreadAsync( () => { _ = SpawnGrasses_thread( grassAmount * 10, grassAmount, hitpositions, hitnormals ); } );
		await SpawnGrasses_thread( grassAmount * 20, grassAmount, hitpositions, hitnormals );

		var count = hitpositions.Count;

		for ( var i = 0; i < count; i++ )
		{
			if ( _debug )
				Logger.Info( "Preparing grass; " + hitpositions[i] );

			doneGrasses.Add( new EntInfo()
			{
				Model = i < count * 0.9f ? Game.Random.FromArray( GrassModels ) : Game.Random.FromArray( (Game.Random.Int( 1, 2 ) == 1) ? BushModels : ((Game.Random.Int( 1, 2 ) == 1) ? FlowerModels : ReedModels) ),
				Scale = Game.Random.Float( i < count * 0.9f ? 0.7f : 0.4f, i < count * 0.9f ? 1.2f : 0.8f ),
				Position = hitpositions[i],
				Rotation = Rotation.LookAt( hitnormals[i] + Vector3.Random * 0.1f, Vector3.Random ) * Rotation.From( 90, 0, 0 ),
				RenderColor = Color.Lerp( "#4c5610", "#c79852", Game.Random.Float( 0.0f, 1f ) ),
			} );
		}

		if ( Cancelled.IsCancellationRequested )
		{
			PAF_Clear();
			return;
		}

		SendGrasses();

		Logger.Info( $"[PAF-SV] Prepared {count} grasses!" );

	}

	//[Rpc.Broadcast]
	public static async void SendGrasses()
	{
		using ( var stream = new MemoryStream() )
		{
			using ( var writer = new BinaryWriter( stream ) )
			{
				writer.Write( doneGrasses.Count );

				foreach ( var item in doneGrasses )
				{
					writer.Write( item.Model );
					writer.Write( item.Scale.ToString() ); //float problem?
					writer.Write( item.Position.ToString() );
					writer.Write( item.Rotation.ToString() );
					writer.Write( item.RenderColor.ToString( true, true ) );
				}

				SendGrassesToCL( stream.ToArray() );
			}
		}
	}

	[Rpc.Broadcast]
	public static async void SendGrassesToCL( byte[] data )
	{
		spawnedGrasses ??= new();

		foreach ( var grass in spawnedGrasses )
			grass.Destroy();

		spawnedGrasses.Clear();

		using ( var stream = new MemoryStream( data ) )
		{
			using ( var reader = new BinaryReader( stream ) )
			{
				var count = reader.ReadInt32();

				for ( var i = 0; i < count; i++ )
				{
					var model = reader.ReadString();
					var scale = float.Parse( reader.ReadString() );
					var position = Vector3.Parse( reader.ReadString() );
					var rotation = Rotation.Parse( reader.ReadString() );
					var rendercolor = Color.Parse( reader.ReadString() );

					var entObj = new GameObject();
					entObj.Tags.Add( "paf" );
					var ent = entObj.AddComponent<ModelRenderer>();
					ent.Model = model.Contains( ".vmdl" ) ? Model.Load( model ) : await DownloadModel( model );
					ent.WorldScale = scale;
					ent.WorldPosition = position;
					ent.WorldRotation = rotation;
					ent.Tint = rendercolor.GetValueOrDefault();

					spawnedGrasses.Add( entObj );
				}

				Logger.Info( $"[PAF-CL] Created {count} grasses!" );
			}
		}
	}

	//[ConCmd( "sv_paf_spawntrees", Flags = ConVarFlags.Admin )]
	public static async Task SpawnTrees()
	{
		spawnedTrees ??= new();

		foreach ( var trees in spawnedTrees )
			trees.Destroy();

		spawnedTrees.Clear();

		int delaystep = 500; //batch
		int delaycount = 0;

		for ( int i = 0; i < treeAmount; i++ )
		{
			if ( delaycount++ == delaystep )
			{
				await GameTask.Delay( 10 );
				delaycount = 0;

				if ( _debug )
					Logger.Info( $"Checked {i}. points" );
			}

			var randomPoint = calculatedBounds.RandomPointInside;

			//z-axis for ground checking
			var angle = Vector3.Down * calculatedHeight;
			angle += randomPoint;

			//z-axis for underground space checking
			//var angleGround = Vector3.Down * 15f;
			//angle += CurrentView.Rotation.Forward;

			//Ground checking
			var tr = Game.ActiveScene.Trace.Ray( randomPoint, angle )
				//.UseRenderMeshes( true )
				.UsePhysicsWorld( true )
				.UseHitboxes( false )
				.UseHitPosition( true )
				.WithoutTags( "<invalid>", "water", "passbullets", "sky", "paf" )
				.Size( new Vector3( 5, 5, 25 ) )//to detect small areas
												//.Radius( 150f )
				.Run();

			if ( !tr.Hit )
				continue;

			//Angles checking for spawning horizontally
			if ( tr.Normal.Angle( Vector3.Up ) > 25.0f )
				continue;

			var hitPos = tr.HitPosition;

			var tr2 = Game.ActiveScene.Scene.Trace // Check the roof from inside
			.Ray( hitPos + Vector3.Up * 150f, hitPos + Vector3.Up * 1000f )
				//.UseRenderMeshes( true )
				.UsePhysicsWorld( true )
				.Size( 140f )
			.Run();

			if ( tr2.Hit )
				continue;

			//var tr3 = Game.ActiveScene.Scene.Trace // Check the ground space
			//.Ray( hitPos + Vector3.Down * 5f, hitPos + Vector3.Down * 10f )
			//	.UseRenderMeshes( true )
			//	.UsePhysicsWorld( true )
			//.Run();

			//if ( tr3.Hit )
			//{
			//	var tr4 = Game.ActiveScene.Scene.Trace // Check the ground space
			//	.Ray( hitPos + Vector3.Down * 200f, hitPos + Vector3.Down * 250f )
			//		.UseRenderMeshes( true )
			//		.UsePhysicsWorld( true )
			//	.Run();

			//	if(!tr4.Hit)
			//		continue;
			//}

			//Distance checking between them
			if ( spawnedTrees.Any( x => x.WorldPosition.DistanceSquared( hitPos ) < 200 * 200 ) )
				continue;

			var entObj = new GameObject();
			entObj.Tags.Add( "paf" );
			var ent = entObj.AddComponent<ModelRenderer>();
			ent.Model = await DownloadModel( Game.Random.FromArray( TreeModels ) );
			ent.WorldScale = Game.Random.Float( 0.7f, 2.6f );
			ent.WorldPosition = hitPos;
			ent.WorldRotation = Rotation.LookAt( Vector3.Up + Vector3.Random * 0.4f, Vector3.Random ) * Rotation.From( 90, 0, 0 );
			ent.Tint = Color.Lerp( "#4c5610", "#c79852", Game.Random.Float( 0.0f, 1f ) );
			var entPhysics = entObj.AddComponent<ModelCollider>(); // TODO: Collider don't working?
			entPhysics.Model = ent.Model;
			spawnedTrees.Add( entObj );
			entObj.Network.DropOwnership();
			entObj.NetworkSpawn();
		}


		if ( Cancelled.IsCancellationRequested )
		{
			PAF_Clear();
			return;
		}

		Logger.Info( $"[PAF-SV] Created {spawnedTrees.Count} trees!" );
	}

	public static async Task SpawnRocks()
	{
		spawnedRocks ??= new();

		foreach ( var rock in spawnedRocks )
			rock.Destroy();

		spawnedRocks.Clear();
		for ( int i = 0; i < rockAmount; i++ )
		{
			var randomPoint = calculatedBounds.RandomPointInside;

			//z-axis for ground checking
			var angle = Vector3.Down * calculatedHeight;
			angle += randomPoint;

			//z-axis for sky checking
			//var angleSky = Vector3.Up * calculatedHeight;
			//angleSky += randomPoint;

			//Ground checking
			var tr = Game.ActiveScene.Trace.Ray( randomPoint, angle )
				//.UseRenderMeshes( true )
				.UsePhysicsWorld( true )
				.UseHitboxes( false )
				.UseHitPosition( true )
				.WithoutTags( "<invalid>", "water", "passbullets", "sky", "paf" )
				.Size( new Vector3( 5, 5, 25 ) )//to detect small areas
												//.Radius( 150f )
				.Run();

			if ( !tr.Hit )
				continue;

			////Sky checking
			//var trSky = Game.ActiveScene.Trace.Ray( tr.HitPosition, tr.HitPosition + (Vector3.Up * calculatedHeight) )
			//	.UseRenderMeshes( true )
			//	.UsePhysicsWorld( false )
			//	.UseHitboxes( false )
			//	.UseHitPosition( true )
			//	// passbullets
			//	.Run();

			//if ( trSky.Hit )
			//	continue;

			//Angles checking for spawning horizontally
			if ( tr.Normal.Angle( Vector3.Up ) > 25.0f )
				continue;
			//Distance checking between them
			if ( spawnedRocks.Any( x => x.WorldPosition.DistanceSquared( tr.HitPosition ) < 200 * 200 ) )
				continue;

			var entObj = new GameObject();
			entObj.Tags.Add( "paf" );
			var ent = entObj.AddComponent<ModelRenderer>();
			ent.Model = await DownloadModel( Game.Random.FromArray( RockModels ) );
			ent.WorldScale = Game.Random.Float( 0.3f, 0.7f );
			ent.WorldPosition = tr.HitPosition;
			ent.WorldRotation = Rotation.LookAt( Vector3.Up + Vector3.Random * 0.4f, Vector3.Random ) * Rotation.From( 90, 0, 0 );
			ent.Tint = Color.Lerp( "#4c5610", "#c79852", Game.Random.Float( 0.0f, 1f ) );
			var entPhysics = entObj.AddComponent<ModelCollider>();
			entPhysics.Model = ent.Model;
			spawnedRocks.Add( entObj );
			entObj.Network.DropOwnership();
			entObj.NetworkSpawn();
		}

		if ( Cancelled.IsCancellationRequested )
		{
			PAF_Clear();
			return;
		}

		Logger.Info( $"[PAF-SV] Created {spawnedRocks.Count} rocks!" );

	}

	private static async Task SpawnIvys_thread( int maxray, int amount, List<Vector3> hitpositions, List<Vector3> hitnormals )
	{
		var founded = 0;
		int delaystep = 500; //batch
		int delaycount = 0;

		for ( int i = 0; i < maxray; i++ )
		{
			if ( delaycount++ == delaystep )
			{
				await GameTask.Delay( 10 );
				delaycount = 0;

				if ( _debug )
					Logger.Info( $"Checked {i}. points" );
			}

			if ( founded >= amount )
				break;

			var randomPoint = calculatedBounds.RandomPointInside;

			//z-axis for sky checking
			var angleSky = Vector3.Up * calculatedHeight;
			angleSky += randomPoint;

			//Ground checking
			var tr = Game.ActiveScene.Trace.Ray( randomPoint, randomPoint * Vector3.Random * calculatedHeight )
				//.UseRenderMeshes( true )
				.UsePhysicsWorld( true )
				.UseHitboxes( false )
				.UseHitPosition( true )
				.WithoutTags( "<invalid>", "water", "passbullets", "sky", "paf" )
				.Run();

			if ( !tr.Hit )
				continue;

			//Sky checking
			var trSky = Game.ActiveScene.Trace.Ray( tr.HitPosition, tr.HitPosition + (Vector3.Up * calculatedHeight) )
				//.UseRenderMeshes( true )
				.UsePhysicsWorld( true )
				.UseHitboxes( false )
				.UseHitPosition( true )
				// passbullets
				.Run();

			if ( trSky.Hit )
				continue;

			//Angles checking for spawning vertically
			if ( tr.Normal.Angle( Vector3.Up ) < 25.0f )
				continue;

			//Distance checking between them
			if ( spawnedIvys.Any( x => x.WorldPosition.Distance( tr.HitPosition ) < 40 ) )
				continue;

			hitpositions.Add( tr.HitPosition );
			hitnormals.Add( tr.Normal );

			++founded;
		}
	}

	public static async Task SpawnIvys()
	{
		spawnedIvys ??= new();

		clearForestCL( ivys: true );

		spawnedIvys.Clear();
		doneIvys.Clear();

		List<Vector3> hitpositions = new();
		List<Vector3> hitnormals = new();

		await SpawnIvys_thread( ivyAmount * 10, ivyAmount, hitpositions, hitnormals );

		var count = hitpositions.Count;

		for ( var i = 0; i < count; i++ )
		{
			if ( _debug )
				Logger.Info( "Preparing ivy; " + hitpositions[i] );

			doneIvys.Add( new EntInfo()
			{
				Model = Game.Random.FromArray( IvyModels ),
				Scale = Game.Random.Float( 0.7f, 1.2f ),
				Position = hitpositions[i],
				Rotation = Rotation.LookAt( hitnormals[i] + Vector3.Random * 0.4f, Vector3.Random ) * Rotation.From( 0, 0, 90 ), //review
				RenderColor = Color.Lerp( "#4c5610", "#c79852", Game.Random.Float( 0.0f, 1f ) ),
			} );

		}

		if ( Cancelled.IsCancellationRequested )
		{
			PAF_Clear();
			return;
		}

		SendIvys();

		Logger.Info( $"[PAF-SV] Prepared {count} ivys!" );
	}

	private static void SendIvys()
	{
		using ( var stream = new MemoryStream() )
		{
			using ( var writer = new BinaryWriter( stream ) )
			{
				writer.Write( doneIvys.Count );

				foreach ( var item in doneIvys )
				{
					writer.Write( item.Model );
					writer.Write( item.Scale.ToString() ); //float problem?
					writer.Write( item.Position.ToString() );
					writer.Write( item.Rotation.ToString() );
					writer.Write( item.RenderColor.ToString( true, true ) );
				}

				SendIvysToCL( stream.ToArray() );
			}
		}
	}

	[Rpc.Broadcast]
	public static async void SendIvysToCL( byte[] data )
	{
		spawnedIvys ??= new();

		foreach ( var ivy in spawnedIvys )
			ivy.Destroy();

		spawnedIvys.Clear();

		using ( var stream = new MemoryStream( data ) )
		{
			using ( var reader = new BinaryReader( stream ) )
			{
				var count = reader.ReadInt32();

				for ( var i = 0; i < count; i++ )
				{
					var model = reader.ReadString();
					var scale = float.Parse( reader.ReadString() );
					var position = Vector3.Parse( reader.ReadString() );
					var rotation = Rotation.Parse( reader.ReadString() );
					var rendercolor = Color.Parse( reader.ReadString() );

					var entObj = new GameObject();
					entObj.Tags.Add( "paf" );
					var ent = entObj.AddComponent<ModelRenderer>();
					ent.Model = model.Contains( ".vmdl" ) ? Model.Load( model ) : await DownloadModel( model );
					ent.WorldScale = scale;
					ent.WorldPosition = position;
					ent.WorldRotation = rotation;
					ent.Tint = rendercolor.GetValueOrDefault();

					spawnedIvys.Add( entObj );
				}

				Logger.Info( $"[PAF-CL] Created {count} Ivys!" );
			}
		}
	}


	private static async Task SpawnDebris_thread( int maxray, int amount, List<Vector3> hitpositions, List<Vector3> hitnormals )
	{
		var founded = 0;
		int delaystep = 10000; //batch
		int delaycount = 0;

		for ( int i = 0; i < maxray; i++ )
		{

			if ( delaycount == delaystep )
			{
				await GameTask.Delay( 10 );
				delaycount = 0;

				if ( _debug )
					Logger.Info( $"Checked {i}. points" );
			}

			delaycount++;

			if ( founded >= amount )
				break;

			var randomPoint = calculatedBounds.RandomPointInside;

			//z-axis for ground checking
			var angle = Vector3.Down * calculatedHeight;
			angle += randomPoint;

			//Ground checking
			var tr = Game.ActiveScene.Trace.Ray( randomPoint, angle )
				//.UseRenderMeshes( true )
				.UsePhysicsWorld( true )
				.UseHitboxes( false )
				.UseHitPosition( true )
				.WithoutTags( "<invalid>", "water", "passbullets", "sky", "paf" )
				.Run();

			if ( !tr.Hit )
				continue;

			//Sky checking
			var trSky = Game.ActiveScene.Trace.Ray( tr.HitPosition, tr.HitPosition + (Vector3.Up * calculatedHeight) )
				//.UseRenderMeshes( true )
				.UsePhysicsWorld( true )
				.UseHitboxes( false )
				.UseHitPosition( true )
				.WithoutTags( "paf" )
				// passbullets
				.Run();

			if ( !trSky.Hit )
				continue;

			//Angles checking for spawning horizontally
			if ( tr.Normal.Angle( Vector3.Up ) > 25.0f )
				continue;

			//Distance checking between them
			if ( spawnedDebris.Any( x => x.WorldPosition.DistanceSquared( tr.HitPosition ) < 100 * 100 ) )
				continue;

			hitpositions.Add( tr.HitPosition );
			hitnormals.Add( tr.Normal );

			++founded;
		}
	}

	public static async Task SpawnDebris()
	{
		spawnedDebris ??= new();

		clearForestCL( debris: true );

		spawnedDebris.Clear();
		doneDebris.Clear();

		List<Vector3> hitpositions = new();
		List<Vector3> hitnormals = new();

		await SpawnDebris_thread( debrisAmount * 1000, debrisAmount, hitpositions, hitnormals );

		var count = hitpositions.Count;

		for ( var i = 0; i < count; i++ )
		{
			if ( _debug )
				Logger.Info( "Preparing debris; " + hitpositions[i] );

			doneDebris.Add( new EntInfo()
			{
				Model = Game.Random.FromArray( DebrisModels ),
				Scale = Game.Random.Float( 0.8f, 1.2f ),
				Position = hitpositions[i],
				Rotation = Rotation.LookAt( hitnormals[i] + Vector3.Random * 0.1f, Vector3.Random ) * Rotation.From( 90, 0, 0 ),
				RenderColor = Color.Lerp( "#4c5610", "#c79852", Game.Random.Float( 0.0f, 1f ) ),
			} );
		}

		if ( Cancelled.IsCancellationRequested )
		{
			PAF_Clear();
			return;
		}

		SendDebris();

		Logger.Info( $"[PAF-SV] Prepared {count} debris!" );
	}

	private static void SendDebris()
	{
		using ( var stream = new MemoryStream() )
		{
			using ( var writer = new BinaryWriter( stream ) )
			{
				writer.Write( doneDebris.Count );

				foreach ( var item in doneDebris )
				{
					writer.Write( item.Model );
					writer.Write( item.Scale.ToString() ); //float problem?
					writer.Write( item.Position.ToString() );
					writer.Write( item.Rotation.ToString() );
					writer.Write( item.RenderColor.ToString( true, true ) );
				}

				SendDebrisToCL( stream.ToArray() );
			}
		}
	}

	[Rpc.Broadcast]
	public static async void SendDebrisToCL( byte[] data )
	{
		spawnedDebris ??= new();

		foreach ( var debris in spawnedDebris )
			debris.Destroy();

		spawnedDebris.Clear();

		using ( var stream = new MemoryStream( data ) )
		{
			using ( var reader = new BinaryReader( stream ) )
			{
				var count = reader.ReadInt32();

				for ( var i = 0; i < count; i++ )
				{
					var model = reader.ReadString();
					var scale = float.Parse( reader.ReadString() );
					var position = Vector3.Parse( reader.ReadString() );
					var rotation = Rotation.Parse( reader.ReadString() );
					var rendercolor = Color.Parse( reader.ReadString() );

					var entObj = new GameObject();
					entObj.Tags.Add( "paf" );
					var ent = entObj.AddComponent<ModelRenderer>();
					ent.Model = model.Contains( ".vmdl" ) ? Model.Load( model ) : await DownloadModel( model );
					ent.WorldScale = scale;
					ent.WorldPosition = position;
					ent.WorldRotation = rotation;
					ent.Tint = rendercolor.GetValueOrDefault();

					spawnedDebris.Add( entObj );
				}

				Logger.Info( $"[PAF-CL] Created {count} Debris!" );
			}
		}
	}
	private static async Task SpawnForest()
	{
		await SpawnTrees(); //svside
		await SpawnRocks(); //svside
		await SpawnGrasses(); //clside
		await SpawnIvys(); //clside
		await SpawnDebris(); //clside
		CreateEffects();//shared
	}

	private static void calculateRatios( bool forceCalculation = false )
	{
		if ( forceCalculation || !firstCalculation )
		{
			calculatedBounds = Game.ActiveScene.GetAllObjects( false ).FirstOrDefault( x => x.Name == "MapLoader" ).GetComponent<MapInstance>().Bounds;
			calculatedHeight = calculatedBounds.Size.z;
			var ratio = (calculatedBounds.Size.x * calculatedBounds.Size.y) / 130347680f; //based construct map

			Logger.Info( $"[PAF] Map scale calculated as {ratio}" );

			ratio += 0.1f; //boost

			treeAmount = Math.Min( (int)(1000 * ratio), 5000 );
			rockAmount = Math.Min( (int)(200 * ratio), 500 );
			grassAmount = Math.Min( (int)(5000 * ratio), 6000 );
			ivyAmount = Math.Min( (int)(2000 * ratio), 3000 );
			debrisAmount = Math.Min( (int)(1000 * ratio), 1000 );

			Logger.Info( $"[PAF] Calculated as Sufficiently for '{Networking.MapName}'; Tree: {treeAmount}, Rock: {rockAmount}, Grass: {grassAmount}, Ivy: {ivyAmount}, Debris: {debrisAmount}" );

			firstCalculation = true;
		}
	}

	public static async Task PAF_Generate()
	{
		if ( Generated )
		{
			InfoClient( "[PAF] Forest already generated. Aborting generation." );

			return;
		}

		float cooldown = 180f;

		if ( (GenerateSpamProtection - Time.Now) > cooldown )
			GenerateSpamProtection = 0;

		if ( GenerateSpamProtection > Time.Now )
		{
			InfoClient( $"[PAF] Please wait {(GenerateSpamProtection - Time.Now).CeilToInt()} seconds before generating the forest again." );
			return;
		}

		Generated = true;
		Cancelled = new();
		GenerateSpamProtection = Time.Now + cooldown; //180 seconds cooldown

		calculateRatios();
		await SpawnForest();
	}

	public static void PAF_Clear()
	{
		if ( spawnedTrees != null )
		{
			foreach ( var trees in spawnedTrees )
				trees.Destroy();

			if ( spawnedTrees.Count > 0 )
				Logger.Info( $"[PAF-SV] Removed {spawnedTrees.Count} trees!" );

			spawnedTrees.Clear();
		}

		if ( doneGrasses != null )
			doneGrasses.Clear();

		if ( doneIvys != null )
			doneIvys.Clear();

		if ( doneDebris != null )
			doneDebris.Clear();

		if ( spawnedRocks != null )
		{
			foreach ( var rock in spawnedRocks )
				rock.Destroy();

			if ( spawnedRocks.Count > 0 )
				Logger.Info( $"[PAF-SV] Removed {spawnedRocks.Count} rocks!" );

			spawnedRocks.Clear();
		}

		var paf_entity = Game.ActiveScene.GetAllObjects( true ).FirstOrDefault( x => x.Name == "paf_entity" && !x.IsProxy );

		if ( paf_entity.IsValid() && paf_entity.GetComponent<GradientFog>( true ) is GradientFog paf_entity_fog_component && paf_entity_fog_component.IsValid() )
			paf_entity_fog_component.Enabled = false;

		if ( paf_entity.IsValid() && paf_entity.GetComponent<AmbientLight>( true ) is AmbientLight paf_entity_color_grading_component && paf_entity_color_grading_component.IsValid() )
			paf_entity_color_grading_component.Enabled = false;

		clearForestCL( grasses: true, ivys: true, debris: true, effects: true );

		Generated = false;
		Cancelled.Cancel();

		removePanel();
	}

	[Rpc.Broadcast]
	public static void clearForestCL( bool grasses = false, bool ivys = false, bool debris = false, bool effects = false )
	{
		if ( grasses && spawnedGrasses != null )
		{
			foreach ( var grass in spawnedGrasses )
				grass.Destroy();

			if ( spawnedGrasses.Count > 0 )
				Logger.Info( $"[PAF-CL] Removed {spawnedGrasses.Count} grasses!" );

			spawnedGrasses.Clear();
		}

		if ( ivys && spawnedIvys != null )
		{
			foreach ( var ivy in spawnedIvys )
				ivy.Destroy();

			if ( spawnedIvys.Count > 0 )
				Logger.Info( $"[PAF-CL] Removed {spawnedIvys.Count} ivys!" );

			spawnedIvys.Clear();
		}

		if ( debris && spawnedDebris != null )
		{
			foreach ( var debr in spawnedDebris )
				debr.Destroy();

			if ( spawnedDebris.Count > 0 )
				Logger.Info( $"[PAF-CL] Removed {spawnedDebris.Count} debris!" );

			spawnedDebris.Clear();
		}

		if ( effects )
		{
			var paf_entity = Game.ActiveScene.GetAllObjects( true ).FirstOrDefault( x => x.Name == "paf_entity" && !x.IsProxy );

			if ( paf_entity.IsValid() && paf_entity.GetComponent<GradientFog>( true ) is GradientFog paf_entity_fog_component && paf_entity_fog_component.IsValid() )
				paf_entity_fog_component.Enabled = false;

			if ( paf_entity.IsValid() && paf_entity.GetComponent<AmbientLight>( true ) is AmbientLight paf_entity_color_grading_component && paf_entity_color_grading_component.IsValid() )
				paf_entity_color_grading_component.Enabled = false;
		}
	}

	// Events and game's overrides can't accessible?

	public void OnConnected( Connection client )
	{
		if ( spamProtect.TryGetValue( client.SteamId, out var cl ) )
		{
			if ( cl.Equals( client ) )
				return;
			else
			{
				spamProtect.Remove( client.SteamId );
				spamProtect.Add( client.SteamId, client );
			}
		}
		else
			spamProtect.Add( client.SteamId, client );

		using ( Rpc.FilterInclude( c => c == client ) )
		{
			if ( doneGrasses.Count > 0 )
				SendGrasses();
			if ( doneIvys.Count > 0 )
				SendIvys();
			if ( doneDebris.Count > 0 )
				SendDebris();
			if ( doneGrasses.Count + doneIvys.Count + doneDebris.Count > 0 )
				CreateEffectsCL();
		}
	}
	static async Task<Model> DownloadModel( string packageIdent )
	{
		var package = await Package.Fetch( packageIdent, false );
		if ( package == null || package.Revision == null )
		{
			// Package not found
			return null;
		}
		// If the package was found, mount it (download the content)
		await package.MountAsync();

		// Get the path to the primary asset (vmdl for Model, vsnd for Sound, ect.)
		var primaryAsset = package.GetMeta( "PrimaryAsset", "" );
		return Model.Load( primaryAsset );
	}

	[Rpc.Broadcast]
	public static void InfoClient( string error = "" )
	{
		Logger.Error( error );
	}
}
