using System.IO;

namespace Server.Mobiles
{
    public static class SwimGrantPersistence
    {
        private static readonly string FilePath = Path.Combine("Saves", "Mounts", "SwimGrants.bin");

        public static void Configure()
        {
            EventSink.WorldSave += OnWorldSave;
            EventSink.WorldLoad += OnWorldLoad;
        }

        private static void OnWorldSave(WorldSaveEventArgs e)
        {
            // Timers do not survive a restart. Persist who is still owed a swim grant so
            // the state does not silently leak into a permanent water-walk on reload.
            Persistence.Serialize(
                FilePath,
                writer =>
                {
                    writer.Write(0); // version

                    var mobiles = BaseMount.SwimGrantRecipients;

                    writer.Write(mobiles.Count);

                    foreach (Mobile m in mobiles)
                    {
                        writer.Write(m);
                    }
                });
        }

        private static void OnWorldLoad()
        {
            Persistence.Deserialize(
                FilePath,
                reader =>
                {
                    int version = reader.ReadInt();

                    switch (version)
                    {
                        case 0:
                            {
                                int count = reader.ReadInt();

                                for (int i = 0; i < count; ++i)
                                {
                                    Mobile m = reader.ReadMobile();

                                    if (m != null && !m.Deleted)
                                        BaseMount.StartSwimGrantTimer(m);
                                }

                                break;
                            }
                    }
                });
        }
    }
}
