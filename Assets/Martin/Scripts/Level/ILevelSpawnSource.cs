// Componentes que generan enemigos contables dentro de un nivel implementan esta interfaz.
// LevelObjectiveManager suma CountLevelEnemies() de todos los componentes de la escena del nivel
// al entrar, para saber el total sin depender de lo que aparezca en runtime.
public interface ILevelSpawnSource
{
    // Enemigos contables que este componente generará en el nivel (valor estático).
    int CountLevelEnemies();
}
