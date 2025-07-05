using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace Ajedrez.Systems
{
    public class AudioSystem : MonoBehaviour
    {
        public enum TipoSonido
        {
            Movimiento,
            Captura,
            Enroque,
            Promocion,
            Jaque
        }

        public enum TipoMusica
        {
            Menu
        }

        public static AudioSystem Instancia { get; private set; }

        // Constantes
        private const float MIN_PITCH = 0.9f;
        private const float MAX_PITCH = 1.1f;

        // Audio sources
        private AudioSource sonidoSource;
        private AudioSource sonidoAleatorioSource;
        private AudioSource musicaSource;

        [Header("Audio Mixer")]
        [SerializeField] private AudioMixer audioMixer;

        [Header("Mixer Groups")]
        [SerializeField] private AudioMixerGroup mixerGrupoEfectos;
        [SerializeField] private AudioMixerGroup mixerGrupoMusica;

        // Audio clips
        [Header("Sonidos")]
        [SerializeField] private AudioClip sonidoMovimiento;
        [SerializeField] private AudioClip sonidoCaptura;
        [SerializeField] private AudioClip sonidoEnroque;
        [SerializeField] private AudioClip sonidoPromocion;
        [SerializeField] private AudioClip sonidoJaque;

        [Header("Música")]
        [SerializeField] private AudioClip musicaMenu;

        private Dictionary<TipoSonido, AudioClip> sonidos;
        private Dictionary<TipoMusica, AudioClip> musicas;

        private void Awake()
        {
            if (Instancia == null)
            {
                Instancia = this;
                DontDestroyOnLoad(gameObject);

                // Crear y configurar los AudioSource por código
                sonidoSource = gameObject.AddComponent<AudioSource>();
                sonidoAleatorioSource = gameObject.AddComponent<AudioSource>();
                musicaSource = gameObject.AddComponent<AudioSource>();
                sonidoSource.playOnAwake = false;
                sonidoAleatorioSource.playOnAwake = false;
                musicaSource.playOnAwake = false;
                musicaSource.loop = true;

                // Asignar Mixer Groups
                sonidoSource.outputAudioMixerGroup = mixerGrupoEfectos;
                sonidoAleatorioSource.outputAudioMixerGroup = mixerGrupoEfectos;
                musicaSource.outputAudioMixerGroup = mixerGrupoMusica;

                InicializarSonidos();
                InicializarMusicas();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void InicializarSonidos()
        {
            sonidos = new Dictionary<TipoSonido, AudioClip>
            {
                { TipoSonido.Movimiento, sonidoMovimiento },
                { TipoSonido.Captura, sonidoCaptura },
                { TipoSonido.Enroque, sonidoEnroque },
                { TipoSonido.Promocion, sonidoPromocion },
                { TipoSonido.Jaque, sonidoJaque }
            };
        }

        private void InicializarMusicas()
        {
            musicas = new Dictionary<TipoMusica, AudioClip>
            {
                { TipoMusica.Menu, musicaMenu }
            };
        }

        public void ReproducirSonido(TipoSonido sonido)
        {
            sonidoSource.PlayOneShot(sonidos[sonido]);
        }

        public void ReproducirSonidoConPitchAleatorio(TipoSonido sonido, float min = MIN_PITCH, float max = MAX_PITCH)
        {
            sonidoAleatorioSource.pitch = Random.Range(min, max);
            sonidoAleatorioSource.PlayOneShot(sonidos[sonido]);
        }

        public void ReproducirMusica(TipoMusica musica)
        {
            musicaSource.clip = musicas[musica];
            musicaSource.Play();
        }

        public void EstablecerVolumenGeneral(float volumen)
        {
            audioMixer.SetFloat("volumenGeneral", Mathf.Log10(volumen) * 20f);
        }

        public void EstablecerVolumenEfectos(float volumen)
        {
            audioMixer.SetFloat("volumenEfectos", Mathf.Log10(volumen) * 20f);
        }

        public void EstablecerVolumenMusica(float volumen)
        {
            audioMixer.SetFloat("volumenMusica", Mathf.Log10(volumen) * 20f);
        }

        public float ObtenerVolumenGeneral()
        {
            audioMixer.GetFloat("volumenGeneral", out float valorDb);
            return Mathf.Pow(10f, valorDb / 20f);
        }

        public float ObtenerVolumenEfectos()
        {
            audioMixer.GetFloat("volumenEfectos", out float valorDb);
            return Mathf.Pow(10f, valorDb / 20f);
        }

        public float ObtenerVolumenMusica()
        {
            audioMixer.GetFloat("volumenMusica", out float valorDb);
            return Mathf.Pow(10f, valorDb / 20f);
        }
    }
}